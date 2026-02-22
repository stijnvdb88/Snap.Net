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

namespace Snap.Net.Avalonia.Services;

public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider m_ServiceProvider;
    private readonly IControlClientService m_ControlClientService;
    private readonly ISettingsService m_SettingsService;
    
    public ApplicationHostService(
        IServiceProvider serviceProvider,
        IControlClientService controlClientService,
        ISettingsService settingsService
        )
    {
        m_ServiceProvider = serviceProvider;
        m_ControlClientService = controlClientService;
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
        if (string.IsNullOrEmpty(host) == false)
        {
            await m_ControlClientService.InitializeAsync(
                host, 
                m_SettingsService.Get<int>(SettingsKeys.PORT, 1705));    
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

        return Task.CompletedTask;
    }

}