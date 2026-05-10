using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts.Services;

namespace Snap.Net.Avalonia.ViewModels.Player;

public class PlayerDeviceSettings
{
    public bool AutoPlay { get; set; }
    public string HostId { get; set; }
    public int Latency { get; set; }
    public string ExtraArgs { get; set; }
    
    public bool AutoRestartOnError { get; set; }
}

public partial class PlayerDeviceViewModel : ObservableObject
{
    private IPlayerService m_PlayerService;
    private ISettingsService m_SettingsService;
    
    [ObservableProperty]
    private string m_FriendlyName;

    [ObservableProperty]
    private string m_Id;

    [ObservableProperty]
    private int m_Index;
    
    private PlayerDeviceSettings m_PlayerSettings = new PlayerDeviceSettings();
    
    public string SettingsKey => SettingsKeys.PLAYER_DEVICE_SETTINGS + "_" + Id + "_" + FriendlyName;

    public string PlayerCommandPreview => m_PlayerService.SnapclientPath + " " + m_PlayerService.GetSnapclientArgs(this);
    
    public bool AutoPlay
    {
        get => m_PlayerSettings.AutoPlay;
        set
        {
            if (m_PlayerSettings.AutoPlay == value) return;
            m_PlayerSettings.AutoPlay = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }
 
    public bool AutoRestartOnError
    {
        get => m_PlayerSettings.AutoRestartOnError;
        set
        {
            if (m_PlayerSettings.AutoRestartOnError == value) return;
            m_PlayerSettings.AutoRestartOnError = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }
    
    public int Latency
    {
        get => m_PlayerSettings.Latency;
        set
        {
            if (m_PlayerSettings.Latency == value) return;
            m_PlayerSettings.Latency = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }
    
    public string HostId
    {
        get => m_PlayerSettings.HostId;
        set
        {
            if (m_PlayerSettings.HostId == value) return;
            m_PlayerSettings.HostId = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }
    
    public string ExtraArgs
    {
        get => m_PlayerSettings.ExtraArgs;
        set
        {
            if (m_PlayerSettings.ExtraArgs == value) return;
            m_PlayerSettings.ExtraArgs = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }
    
    public bool IsPlaying => m_PlayerService.IsPlaying(this);

    public FontWeight NameFontWeight => IsPlaying ? FontWeight.SemiBold : FontWeight.Normal;
    public IBrush NameForeground => IsPlaying ? Brushes.Orange : Brushes.White;

    public PlayerDeviceViewModel(ISettingsService settingsService, IPlayerService playerService, int index, string id, string friendlyName)
    {
        m_SettingsService = settingsService;
        m_PlayerService  = playerService;
        m_Index = index;
        m_Id = id;
        m_FriendlyName = friendlyName;
        _LoadSettings();
    }

    public override bool Equals(object? obj)
    {
        if (obj is PlayerDeviceViewModel playerDeviceViewModel)
        {
            return Equals(playerDeviceViewModel);
        }
        return false;
    }

    protected bool Equals(PlayerDeviceViewModel other)
    {
        return FriendlyName == other.FriendlyName && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FriendlyName, Id);
    }

    [RelayCommand]
    public void SaveSettings()
    {
        m_SettingsService.Set(SettingsKey, m_PlayerSettings);
        OnPropertyChanged(nameof(PlayerCommandPreview));
    }

    private void _LoadSettings()
    {
         m_PlayerSettings = m_SettingsService.Get<PlayerDeviceSettings>(SettingsKey, new PlayerDeviceSettings()) ?? new PlayerDeviceSettings();
    }
    
    public override string ToString()
    {
        return FriendlyName;
    }

    [RelayCommand]
    public void OpenSettings()
    {
        
    }

    [RelayCommand]
    public void TogglePlay()
    {
        m_PlayerService.TogglePlay(this);
        NotifyPlayingStateChanged();
    }
    
    public void NotifyPlayingStateChanged()
    {
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(NameFontWeight));
        OnPropertyChanged(nameof(NameForeground));
    }
    
}