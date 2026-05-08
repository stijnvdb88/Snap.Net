using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Snap.Net.Avalonia.ViewModels.Player;

public partial class PlayerDeviceViewModel : ObservableObject
{
    [ObservableProperty]
    private string m_FriendlyName;

    [ObservableProperty]
    private string m_Id;

    [ObservableProperty]
    private int m_Index;

    [ObservableProperty]
    private bool m_AutoPlay;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameFontWeight))]
    [NotifyPropertyChangedFor(nameof(NameForeground))]
    private bool m_IsPlaying;

    public FontWeight NameFontWeight => IsPlaying ? FontWeight.SemiBold : FontWeight.Normal;
    public IBrush NameForeground => IsPlaying ? Brushes.Orange : Brushes.White;

    public PlayerDeviceViewModel(int index, string id, string friendlyName)
    {
        m_Index = index;
        m_Id = id;
        m_FriendlyName = friendlyName;
    }

    public override string ToString()
    {
        return FriendlyName;
    }

    [RelayCommand]
    public void OpenSettings()
    {
        
    }
}