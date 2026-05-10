using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Microsoft.Extensions.Hosting;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.ViewModels;
using Snap.Net.Avalonia.ViewModels.Player;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Services;

public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider m_ServiceProvider;
    private readonly IControlClientService m_ControlClientService;
    private readonly IPlayerService m_PlayerService;
    private readonly IBroadcastService m_BroadcastService;
    private readonly ISettingsService m_SettingsService;
    
    public ApplicationHostService(
        IServiceProvider serviceProvider,
        IControlClientService controlClientService,
        IPlayerService playerService,
        IBroadcastService broadcastService,
        ISettingsService settingsService
        )
    {
        m_ServiceProvider = serviceProvider;
        m_ControlClientService = controlClientService;
        m_PlayerService = playerService;
        m_BroadcastService = broadcastService;
        m_SettingsService = settingsService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync();
        await HandleActivationAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private async Task InitializeAsync()
    {
        string? host = m_SettingsService.Get<string>(SettingsKeys.HOST);
        if (string.IsNullOrEmpty(host))
        {
            SnapserverEndpoint[] servers = await m_PlayerService.DiscoverSnapserversAsync(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token).ConfigureAwait(false);
            if (servers.Length == 1)
            {
                host = servers[0].Host;
                m_SettingsService.Set(SettingsKeys.HOST, host);
            }
            else
            {
                // todo: open settings window with dropdown available for multiple server options?
            }
        }
        
        if (string.IsNullOrEmpty(host) == false)
        {
            // connect to control port
            await m_ControlClientService.InitializeAsync(
                host, 
                m_SettingsService.Get<int>(SettingsKeys.CONTROL_PORT, 1705));
            
            // start auto-play devices if needed
            PlayerDeviceViewModel[] players = await m_PlayerService.GetDevicesAsync();
            foreach (PlayerDeviceViewModel playerDevice in players)
            {
                if (playerDevice.AutoPlay)
                {
                    _ = m_PlayerService.Play(playerDevice);
                }
            }

            // start auto broadcast if needed
            if (m_SettingsService.Get<bool>(SettingsKeys.BROADCAST_AUTO_START))
            {
                string? broadcastDeviceId = m_SettingsService.Get<string>(SettingsKeys.BROADCAST_DEVICE_ID);
                if (string.IsNullOrEmpty(broadcastDeviceId) == false)
                {
                    await m_BroadcastService.StartBroadcast(host, m_SettingsService.Get<int>(SettingsKeys.BROADCAST_PORT), broadcastDeviceId).ConfigureAwait(false);
                }
            }
        }
    }

    private Task HandleActivationAsync()
    {
        if (App.Current != null)
        {
            if (Application.Current is App app)
            {
                app.DataContext = m_ServiceProvider.GetService(typeof(AppViewModel));
            }
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
        // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
        DisableAvaloniaDataAnnotationValidation();
        return Task.CompletedTask;
    }
    
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}