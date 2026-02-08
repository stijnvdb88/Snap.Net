using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public abstract partial class VolumeControlViewModel : ObservableObject
{
    protected float m_Volume;

    protected bool m_Muted;
    
    [ObservableProperty] 
    protected string? m_Name;

    public float Volume
    {
        get => m_Volume;
        set
        {
            SetProperty(ref m_Volume, value);
            _OnVolumeChanged((int)value);
        }
    }

    public bool Muted
    {
        get => m_Muted;
        set
        {
            SetProperty(ref m_Muted, !value);
        }
    }

    protected abstract void _OnVolumeChanged(int value);

    [RelayCommand]
    public abstract void MuteToggled();
}