using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
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
        string? appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
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
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHostedService<ApplicationHostService>();
        
        // Core services
        services.AddSingleton<IControlClientService, ControlClientService>();
        
        services.AddTransient<AppViewModel>();

        services.AddTransient<GroupViewModel>();
        services.AddTransient<GroupViewModel>();

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