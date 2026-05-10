using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Net.Avalonia.Contracts.Services;

namespace Snap.Net.Avalonia.ViewModels.Player;

public partial class PlayerDeviceViewModel : ObservableObject
{
    private IPlayerService m_PlayerService;
    
    [ObservableProperty]
    private string m_FriendlyName;

    [ObservableProperty]
    private string m_Id;

    [ObservableProperty]
    private int m_Index;

    [ObservableProperty]
    private bool m_AutoPlay;
    
    public bool IsPlaying => m_PlayerService.IsPlaying(this);

    public FontWeight NameFontWeight => IsPlaying ? FontWeight.SemiBold : FontWeight.Normal;
    public IBrush NameForeground => IsPlaying ? Brushes.Orange : Brushes.White;

    public PlayerDeviceViewModel(IPlayerService playerService, int index, string id, string friendlyName)
    {
        m_PlayerService  = playerService;
        m_Index = index;
        m_Id = id;
        m_FriendlyName = friendlyName;
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