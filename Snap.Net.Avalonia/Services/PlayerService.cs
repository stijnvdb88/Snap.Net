using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Utils;
using Snap.Net.Avalonia.ViewModels.Player;
using Zeroconf;

namespace Snap.Net.Avalonia.Services;

public class PlayerService : IPlayerService
{
    private class ActivePlayer
    {
        public CommandTask<CommandResult> PlayTask;
        public CancellationTokenSource CancellationTokenSource;
    }
    
    private readonly IServiceProvider m_ServiceProvider;
    private readonly ISettingsService m_SettingsService;
    private Dictionary<PlayerDeviceViewModel, ActivePlayer> m_ActivePlayers = new Dictionary<PlayerDeviceViewModel, ActivePlayer>();
    private const int MINIMUM_INSTANCE_ID = 72;

    private PlayerDeviceViewModel[] m_Devices =  Array.Empty<PlayerDeviceViewModel>();

    public string SnapclientPath
    {
        get
        {
            string? settingsValue = m_SettingsService.Get<string>(SettingsKeys.SNAPCLIENT_PATH, null);
            string binaryName = OperatingSystem.IsWindows() ? "snapclient.exe" : "snapclient";
            if (string.IsNullOrEmpty(settingsValue))
            {
                return Path.Combine(AppContext.BaseDirectory, "snapclient", binaryName);
            }
            return settingsValue;
        }
    }

    public string? SnapclientVersion { get; private set; }
    
    public PlayerService(IServiceProvider serviceProvider, ISettingsService settingsService)
    {
        m_ServiceProvider = serviceProvider;
        m_SettingsService = settingsService;
    }

    public async Task ValidateSnapclientPath()
    {
        if (File.Exists(SnapclientPath))
        {
            BufferedCommandResult result = await Cli.Wrap(SnapclientPath)
                .WithArguments("--version")
                .ExecuteBufferedAsync();
            string version = result.StandardOutput.Split(Environment.NewLine)[0];
            if (version.StartsWith("snapclient"))
            {
                SnapclientVersion = version.Split(' ')[1];
                return;
            }
        }

        SnapclientVersion = null;
    }
    
    public async Task<SnapserverEndpoint[]> DiscoverSnapserversAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<IZeroconfHost>? results = await ZeroconfResolver.ResolveAsync("_snapcast._tcp.local.", cancellationToken: cancellationToken);
        List<SnapserverEndpoint> list = new List<SnapserverEndpoint>();
        foreach (IZeroconfHost host in results)
        {
            IService? service = host.Services.Values.FirstOrDefault();
            int port = service?.Port ?? 1704;
            list.Add(new SnapserverEndpoint(host.IPAddress, port));
        }
        return list.ToArray();
    }
    
    public async Task<PlayerDeviceViewModel[]> GetDevicesAsync(bool includeDefault = false)
    {
        BufferedCommandResult result = await Cli.Wrap(SnapclientPath).WithArguments("--list")
            .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);
        m_Devices = _GetFromSnapClientListOutput(result.StandardOutput, includeDefault);
        return m_Devices;
    }

    /// <summary>
    /// grabs whatever instance id we have previously registered for this device.
    /// if none exists yet, find highest registered instance id and +1.
    /// arbitrary minimum instance id is used to try and avoid collisions
    /// with user-defined instance ids 
    /// </summary>
    /// <param name="playerDevice"></param>
    /// <returns></returns>
    private int _GetInstanceId(PlayerDeviceViewModel playerDevice)
    {
        Dictionary<string, int>? instanceIds =
            m_SettingsService.Get(SettingsKeys.DEVICE_INSTANCE_IDS, new Dictionary<string, int>());
        if (instanceIds == null)
        {
            instanceIds = new Dictionary<string, int>();
        }

        if (instanceIds.ContainsKey(playerDevice.SettingsKey) == false)
        {
            // didn't have this one yet, find an id for it + register
            // DefaultIfEmpty(MINIMUM_INSTANCE_ID) is how we try and avoid collusions with user-defined instance ids 
            int instanceId = instanceIds.Values.DefaultIfEmpty(MINIMUM_INSTANCE_ID).Max();
 
            instanceIds.Add(playerDevice.SettingsKey, instanceId+1);
            m_SettingsService.Set(SettingsKeys.DEVICE_INSTANCE_IDS, instanceIds);
        }
        return instanceIds[playerDevice.SettingsKey];
    }
    
    public string GetSnapclientArgs(PlayerDeviceViewModel playerDevice)
    {
        string latency = "";
        if (playerDevice.Latency != 0)
        {
            latency = $"--latency {playerDevice.Latency} ";
        }

        string hostId = "";
        string instance = "";
        if (string.IsNullOrEmpty(playerDevice.HostId))
        {
            // omit instance id if HostID is explicitly set (see https://github.com/stijnvdb88/Snap.Net/issues/19)
            int instanceId = _GetInstanceId(playerDevice);
            instance = $"--instance {instanceId} ";
        }
        else
        {
            hostId = $"--hostID {playerDevice.HostId} ";
        }
        
        string extraArgs = "";
        if (string.IsNullOrEmpty(playerDevice.ExtraArgs) == false)
        {
            extraArgs = $"{playerDevice.ExtraArgs} ";
        }
        
        return $"--soundcard {playerDevice.Index} " +
               latency +
               instance +
               hostId +
               extraArgs +
               $"tcp://{m_SettingsService.Get<string>(SettingsKeys.HOST)}:" +
               $"{m_SettingsService.Get<int>(SettingsKeys.PLAYER_PORT, 1704)} ";
    }

    public async Task Play(PlayerDeviceViewModel playerDevice)
    {
        if (IsPlaying(playerDevice))
        {
            return;
        }
        
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        string args = GetSnapclientArgs(playerDevice);
        Console.WriteLine($"snapclient.exe {args}");
        Command command = Cli.Wrap(SnapclientPath)
            .WithArguments(args);
        CommandTask<CommandResult> commandTask = command.ExecuteAsync(cancellationTokenSource.Token);
        ChildProcessTracker.AddProcess(Process.GetProcessById(commandTask.ProcessId)); // this utility helps us make sure the player process doesn't keep going if our process is killed / crashes
        m_ActivePlayers[playerDevice] = new ActivePlayer()
        {
            CancellationTokenSource = cancellationTokenSource,
            PlayTask = commandTask,
        };

        try
        {
            await commandTask;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            _Stop(playerDevice);
            if (playerDevice.AutoRestartOnError)
            {
                // auto restart if needed
                _ = Play(playerDevice);
            }
        }
    }
    
    public async Task TogglePlay(PlayerDeviceViewModel playerDevice)
    {
        if (IsPlaying(playerDevice))
        {
            _Stop(playerDevice);
        }
        else
        {
            await Play(playerDevice);
        }
    }

    private void _Stop(PlayerDeviceViewModel playerDevice)
    {
        if (IsPlaying(playerDevice))
        {
            m_ActivePlayers[playerDevice].CancellationTokenSource.Cancel();
            m_ActivePlayers.Remove(playerDevice);
        }
    }

    public bool IsPlaying(PlayerDeviceViewModel playerDevice)
    {
        return m_ActivePlayers.ContainsKey(playerDevice);
    }
    
    private PlayerDeviceViewModel[] _GetFromSnapClientListOutput(string output, bool includeDefault)
    {
        List<PlayerDeviceViewModel> devices = new List<PlayerDeviceViewModel>();
        string[] blocks = output
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (string block in blocks)
        {
            string[] lines = block.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            string[] split = lines[0].Split(": ",  StringSplitOptions.None);
            int.TryParse(split[0], CultureInfo.InvariantCulture, out int index);
            string name = split[1];
            string description = lines[1];
            if (includeDefault == true || index > 0)
            {
                if (_IsRelevantDevice(name))
                {
                    devices.Add(ActivatorUtilities.CreateInstance<PlayerDeviceViewModel>(m_ServiceProvider, index, name,  description));    
                }
            }
        }
        return devices.ToArray();
    }
    
    private static bool _IsRelevantDevice(string name)
    {
#if LINUX        
        if (name == "default" || name == "pipewire" || name == "pulse")
            return true;
        if (name.StartsWith("plughw:"))
            return true;
        return false;
#else
        return true;
#endif
    }
}