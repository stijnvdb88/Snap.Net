using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Classic.Avalonia.Theme;
using Material.Styles.Themes;
using Microsoft.Extensions.Configuration;
using Snap.Net.Avalonia.ViewModels;
using Snap.Net.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Semi.Avalonia;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Enums;
using Snap.Net.Avalonia.Services;
using Snap.Net.Avalonia.ViewModels.Broadcast;
using Snap.Net.Avalonia.ViewModels.ControlClient;

namespace Snap.Net.Avalonia;

public partial class App : Application
{
    private IHost? m_Host;
    private readonly Styles m_ThemeStylesContainer = new Styles();
    private FluentTheme? m_FluentTheme;
    private SimpleTheme? m_SimpleTheme;
    private ClassicTheme? m_ClassicTheme;
    private MaterialTheme? m_MaterialTheme;
    private SemiTheme? m_SemiTheme;
    
    private TrayIcon? m_TrayIcon;
    
    private EAppTheme m_PreviousTheme = EAppTheme.Fluent;
    public EAppTheme PreviousTheme => m_PreviousTheme;
    public static EAppTheme CurrentTheme => ((App)Current!).m_PreviousTheme; 
    
    public override void Initialize()
    {
        Styles.Add(m_ThemeStylesContainer);
        AvaloniaXamlLoader.Load(this);
        
        m_FluentTheme = Resources["FluentTheme"] as FluentTheme;
        m_SimpleTheme = Resources["SimpleTheme"] as SimpleTheme;
        m_ClassicTheme = Resources["ClassicTheme"] as ClassicTheme;
        m_MaterialTheme = Resources["MaterialTheme"] as MaterialTheme;
        m_SemiTheme = Resources["SemiTheme"] as SemiTheme;
        //Resources.ThemeDictionaries[ThemeVariant.Light].
        SetAppTheme(EAppTheme.Fluent);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        // The below check prevents the Avalonia previewer from booting up the rest of the software,
        // particularly the auto-broadcast feature.
        // During development of this feature I had it configured to auto-broadcast my microphone to a set of speakers
        // in the room I'm in. It would start broadcasting "out of nowhere" even while the app wasn't running.
        // Rebooted my machine, opened Rider, and before I even ran the app again I could hear it broadcasting again.
        // I'd try a fix, then hear sounds of me typing still come out the speakers, mutter curse words
        // under my breath, only to hear them echo right back at me.
        // 10/10 the funniest bug I've ever had the pleasure of troubleshooting
        if (Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }
        
        string? appLocation = Path.GetDirectoryName(System.AppContext.BaseDirectory);
        m_Host = Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(c =>
            {
                if (appLocation is not null)
                {
                    c.SetBasePath(appLocation);    
                }
            })
            .ConfigureServices(ConfigureServices)
            .Build();
        
        await m_Host.StartAsync();
        m_TrayIcon = TrayIcon.GetIcons(this)?.FirstOrDefault();
        AppViewModel appViewModel = m_Host.Services.GetRequiredService<AppViewModel>();  
        appViewModel.OnTrayIconChanged += _OnTrayIconChanged;
        base.OnFrameworkInitializationCompleted();
    }
    
    private void _OnTrayIconChanged(bool broadcasting)
    {
        Dispatcher.UIThread.Post(() =>
        {
            m_TrayIcon!.Icon = new WindowIcon(
                AssetLoader.Open(new Uri($"avares://Snap.Net.Avalonia/Assets/{(broadcasting ? "snapcast_r.ico" : "snapcast.ico")}")));
        });
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHostedService<ApplicationHostService>();
        
        // Core services
        services.AddSingleton<IControlClientService, ControlClientService>();
        services.AddSingleton<IBroadcastService, BroadcastService>();
        
        services.AddSingleton<AppViewModel>();

        services.AddTransient<GroupViewModel>();
        services.AddTransient<GroupViewModel>();

        services.AddTransient<AudioDeviceViewModel>();
        
        services.AddTransient<FlyoutWindow>();
        services.AddTransient<FlyoutWindowViewModel>();

        services.AddTransient<EditGroupWindow>();
        services.AddTransient<EditGroupWindowViewModel>();
        
        services.AddTransient<EditControlClientWindow>();
        services.AddTransient<EditControlClientWindowViewModel>();
        
        services.AddTransient<StreamWindow>();
        services.AddTransient<StreamWindowViewModel>();

        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsWindowViewModel>();
        
        services.AddTransient<BroadcastWindow>();
        services.AddTransient<BroadcastWindowViewModel>();
        
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISettingsService, SettingsService>();
    }
    
    private static Styles GetTheme(EAppTheme theme)
    {
        App app = (App)Current!;
        switch (theme)
        {
            case EAppTheme.Fluent:
                return app.m_FluentTheme!;
#if THEMING_SUPPORT
            case EAppTheme.Simple:
                return app.m_SimpleTheme!;
            case EAppTheme.Classic:
                return app.m_ClassicTheme!;
            case EAppTheme.Material:
                return app.m_MaterialTheme!;
            case EAppTheme.Semi:
                return app.m_SemiTheme!;
#endif            
            default:
                return app.m_FluentTheme!;
        }
    }
    
    public void SetAppTheme(EAppTheme appTheme)
    {
        if (m_ThemeStylesContainer.Count == 0)
        {
            m_ThemeStylesContainer.Add(new Style());
            // add more here if we need colorpicker/datagrid themes too
        }
        m_ThemeStylesContainer[0] = GetTheme(appTheme);
        m_PreviousTheme = appTheme;
    }

}