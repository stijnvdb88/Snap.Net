using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.ViewModels.Broadcast;

public partial class AudioDeviceViewModel : ObservableObject
{
    private IAudioDevice m_AudioDevice;

    [ObservableProperty]
    private string m_FriendlyName;

    [ObservableProperty]
    private string m_Id;
    
    public AudioDeviceViewModel(IAudioDevice audioDevice)
    {
        m_AudioDevice = audioDevice;
        FriendlyName = m_AudioDevice.FriendlyName;
        Id = m_AudioDevice.Id;
    }

    public override string ToString()
    {
        return FriendlyName;
    }
}