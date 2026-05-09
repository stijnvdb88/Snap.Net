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
    private int? m_PlayerPort;
    
    [ObservableProperty]
    private int? m_ControlPort;
    
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
        PlayerPort = 1704;
        ControlPort = 1705;
        ApplicationVersion = "0.34.0.1";
    }
#endif    
    
    public SettingsWindowViewModel(ISettingsService settingsService, IControlClientService controlClientService)
    {
        m_SettingsService = settingsService;
        m_ControlClientService = controlClientService;
        Host = m_SettingsService.Get<string>(SettingsKeys.HOST);
        PlayerPort = m_SettingsService.Get<int>(SettingsKeys.PLAYER_PORT, 1704);
        ControlPort = m_SettingsService.Get<int>(SettingsKeys.CONTROL_PORT, 1705);
        ShowDisconnectedClients = m_SettingsService.Get<bool>(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, false);
        PanelPosition = m_SettingsService.Get<EPanelPosition>(SettingsKeys.PANEL_POSITION);
        AssemblyInformationalVersionAttribute? infoVersion = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault()!;
        ApplicationVersion = infoVersion?.InformationalVersion.Split('+')[0] ?? "Unknown";
    }

    [RelayCommand]
    public void Save(ICloseable closeable)
    {
        m_SettingsService.Set(SettingsKeys.HOST, Host);
        m_SettingsService.Set(SettingsKeys.PLAYER_PORT, PlayerPort);
        m_SettingsService.Set(SettingsKeys.CONTROL_PORT, ControlPort);
        m_SettingsService.Set(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, ShowDisconnectedClients);
        m_SettingsService.Set(SettingsKeys.PANEL_POSITION, PanelPosition);
        if (string.IsNullOrEmpty(Host) == false && ControlPort != null)
        {
            m_ControlClientService.InitializeAsync(Host, (int)ControlPort).ConfigureAwait(false);    
        }
        closeable.Close();
    }
}