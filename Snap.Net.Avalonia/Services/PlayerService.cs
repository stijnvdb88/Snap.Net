
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Snap.Net.Avalonia.Broadcast;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.ViewModels.Player;

namespace Snap.Net.Avalonia.Services;

public class PlayerService : IPlayerService
{
    private string m_SnapclientPath = "snapclient";
    
    public async Task<PlayerDeviceViewModel[]> GetDevicesAsync(bool includeDefault = false)
    {
        BufferedCommandResult result = await Cli.Wrap(m_SnapclientPath).WithArguments("--list")
            .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);
        return _GetFromSnapClientListOutput(result.StandardOutput, includeDefault);
    }
    
    private static PlayerDeviceViewModel[] _GetFromSnapClientListOutput(string output, bool includeDefault)
    {
        List<PlayerDeviceViewModel> devices = new List<PlayerDeviceViewModel>();
        string[] blocks = output.Split("\n\n",  StringSplitOptions.RemoveEmptyEntries);
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
                    devices.Add(new PlayerDeviceViewModel(index, name, description));    
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