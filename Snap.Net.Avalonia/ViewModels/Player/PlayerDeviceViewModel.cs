using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Net.Avalonia.ViewModels.Player;

public partial class PlayerDeviceViewModel : ObservableObject
{
    [ObservableProperty]
    private string m_FriendlyName;

    [ObservableProperty]
    private string m_Id;

    [ObservableProperty]
    private int m_Index;

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
}