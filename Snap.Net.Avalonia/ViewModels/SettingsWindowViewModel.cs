using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
    private IPlayerService m_PlayerService;
    private IStorageService m_StorageService;

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
    
    [ObservableProperty] 
    private IBrush m_SnapclientVersionForeground = Brushes.Gray;
    
    private bool m_UseBundledSnapclient;
    
    public bool UseBundledSnapclient
    {
        get => m_UseBundledSnapclient;
        set
        {
            if (value && !m_UseBundledSnapclient)
            {
                m_SettingsService.Set<string?>(SettingsKeys.SNAPCLIENT_PATH, null);
                OnPropertyChanged(nameof(SnapclientPath));
                _ = _UpdateSnapclientVersionNumber();
            }
            
            SetProperty(ref m_UseBundledSnapclient, value);   
        }
    }
    
    public string? SnapclientPath => m_PlayerService.SnapclientPath;
    public string? SnapclientVersion
    {
        get
        {
            string version = m_PlayerService.SnapclientVersion;
            if (string.IsNullOrEmpty(version))
            {
                return "Invalid";
            }
            return version;
        }
    }
    
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
    
    public SettingsWindowViewModel(ISettingsService settingsService,
        IStorageService storageService,
        IControlClientService controlClientService,
        IPlayerService playerService)
    {
        m_PlayerService = playerService;
        m_SettingsService = settingsService;
        m_StorageService = storageService;
        m_ControlClientService = controlClientService;
        Host = m_SettingsService.Get<string>(SettingsKeys.HOST);
        PlayerPort = m_SettingsService.Get<int>(SettingsKeys.PLAYER_PORT, 1704);
        ControlPort = m_SettingsService.Get<int>(SettingsKeys.CONTROL_PORT, 1705);
        ShowDisconnectedClients = m_SettingsService.Get<bool>(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, false);
        PanelPosition = m_SettingsService.Get<EPanelPosition>(SettingsKeys.PANEL_POSITION);
        AssemblyInformationalVersionAttribute? infoVersion = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault()!;
        ApplicationVersion = infoVersion?.InformationalVersion.Split('+')[0] ?? "Unknown";
        _ = _UpdateSnapclientVersionNumber();
    }

    private async Task _UpdateSnapclientVersionNumber()
    {
        await m_PlayerService.ValidateSnapclientPath();
        OnPropertyChanged(nameof(SnapclientVersion));
        UseBundledSnapclient = m_PlayerService.SnapclientVersion != null &&
                               string.IsNullOrEmpty(m_SettingsService.Get<string>(SettingsKeys.SNAPCLIENT_PATH));
    }
    
    [RelayCommand]
    private async Task BrowseSnapclient()
    {
        string[]? extensions = OperatingSystem.IsWindows() ? new[] { "exe" } : null;
        string? path = await m_StorageService.OpenFilePickerAsync("Select snapclient executable", extensions);
        if (path != null)
        {
            m_SettingsService.Set(SettingsKeys.SNAPCLIENT_PATH, path);
            // check if valid, update version label
            await m_PlayerService.ValidateSnapclientPath();
            OnPropertyChanged(nameof(SnapclientPath));
            OnPropertyChanged(nameof(SnapclientVersion));

            if (m_PlayerService.SnapclientVersion != null)
            {
                // also check if players are active and restart them?    
            }
        }
    }
    
    

    [RelayCommand]
    public void Save(ICloseable closeable)
    {
        m_SettingsService.Set(SettingsKeys.HOST, Host);
        m_SettingsService.Set(SettingsKeys.PLAYER_PORT, PlayerPort);
        m_SettingsService.Set(SettingsKeys.CONTROL_PORT, ControlPort);
        m_SettingsService.Set(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, ShowDisconnectedClients);
        m_SettingsService.Set(SettingsKeys.PANEL_POSITION, PanelPosition);
        
        // todo: also read directly from text input field and save path set there
        
        if (string.IsNullOrEmpty(Host) == false && ControlPort != null)
        {
            m_ControlClientService.InitializeAsync(Host, (int)ControlPort).ConfigureAwait(false);    
        }
        closeable.Close();
    }
}