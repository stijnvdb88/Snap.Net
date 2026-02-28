using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Enums;

namespace Snap.Net.Avalonia.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private ISettingsService m_SettingsService;
    private IControlClientService m_ControlClientService;

    [ObservableProperty]
    private string? m_Host;
    
    [ObservableProperty]
    private int? m_Port;
    
    [ObservableProperty]
    private bool m_ShowDisconnectedClients;
    
    [ObservableProperty]
    private EPanelPosition m_PanelPosition;

    [ObservableProperty]
    private string m_ApplicationVersion;

    public EPanelPosition[] AvailablePanelPositions => Enum.GetValues<EPanelPosition>();
    
#if DEBUG
    public SettingsWindowViewModel()
    {
        Host = "192.168.1.111";
        Port = 1705;
        ApplicationVersion = "0.34.0.1";
    }
#endif    
    
    public SettingsWindowViewModel(ISettingsService settingsService, IControlClientService controlClientService)
    {
        m_SettingsService = settingsService;
        m_ControlClientService = controlClientService;
        Host = m_SettingsService.Get<string>(SettingsKeys.HOST);
        Port = m_SettingsService.Get<int>(SettingsKeys.PORT, 1705);
        ShowDisconnectedClients = m_SettingsService.Get<bool>(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, false);
        PanelPosition = m_SettingsService.Get<EPanelPosition>(SettingsKeys.PANEL_POSITION);
        AssemblyInformationalVersionAttribute? infoVersion = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault()!;
        ApplicationVersion = infoVersion?.InformationalVersion.Split('+')[0] ?? "Unknown";
    }

    [RelayCommand]
    public void Save(ICloseable closeable)
    {
        m_SettingsService.Set(SettingsKeys.HOST, Host);
        m_SettingsService.Set(SettingsKeys.PORT, Port);
        m_SettingsService.Set(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, ShowDisconnectedClients);
        m_SettingsService.Set(SettingsKeys.PANEL_POSITION, PanelPosition);
        if (string.IsNullOrEmpty(Host) == false && Port != null)
        {
            m_ControlClientService.InitializeAsync(Host, (int)Port).ConfigureAwait(false);    
        }
        closeable.Close();
    }
}