using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Net.Avalonia.Helpers;
using Stream = SnapDotNet.ControlClient.JsonRpcData.Stream;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class StreamViewModel : ObservableObject
{
    [ObservableProperty]
    private string m_NowPlaying = "";

    [ObservableProperty]
    private Bitmap? m_AlbumArt;

    [ObservableProperty]
    private string? m_Name;

    [ObservableProperty]
    private bool m_CanControl;

    [ObservableProperty]
    private bool m_CanGoPrevious;
    
    [ObservableProperty]
    private bool m_CanGoNext;

    [ObservableProperty]
    private bool m_CanPause;

    [ObservableProperty] 
    private bool m_CanPlay;

    [ObservableProperty]
    private bool m_CanPlayPause;

    [ObservableProperty] 
    private bool m_StreamActive;
    
    private Stream? m_Stream;
    
    public Stream? Stream => m_Stream;
    
    public StreamViewModel(Stream stream)
    {
        m_Stream = stream; 
        m_Stream.SERVER_OnStreamPropertiesUpdated += _OnStreamPropertiesUpdated;
        m_Stream.SERVER_OnStreamUpdated += _OnStreamUpdated;
        _OnStreamPropertiesUpdated();
    }

    private void _OnStreamPropertiesUpdated()
    {
        if (m_Stream == null)
        {
            return;
        }

        Name = m_Stream.id;
        StreamActive = m_Stream.status == "playing";
        if (m_Stream?.properties == null || m_Stream.properties.metadata == null)
        {
            NowPlaying = "";
            return;
        }
        
        NowPlaying = m_Stream.properties.metadata.GetNowPlaying();
        if (string.IsNullOrEmpty(m_Stream.properties.metadata.artData?.data) == false)
        {
            AlbumArt = ImageHelpers.ImageFromString(m_Stream.properties.metadata.artData.data, m_Stream.properties.metadata.artData.extension);    
        }
        if (AlbumArt == null && string.IsNullOrEmpty(m_Stream.properties.metadata.artUrl) == false)
        {
            _GetCoverArtFromUrlAsync(m_Stream.properties.metadata.artUrl).ConfigureAwait(false);
        }

        CanControl = m_Stream.properties.canControl;
        CanGoNext = m_Stream.properties.canGoNext;
        CanGoPrevious = m_Stream.properties.canGoPrevious;
        CanPause = m_Stream.properties.canPause && m_Stream.properties.canPlay == false; // only show if we can't do both
        CanPlay = m_Stream.properties.canPlay && m_Stream.properties.canPause == false; // only show if we can't do both
        CanPlayPause = m_Stream.properties.canPause && m_Stream.properties.canPlay; // only show if we can do both
        
    }

    private async Task _GetCoverArtFromUrlAsync(string url)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            byte[] data = await httpClient.GetByteArrayAsync(url);
            MemoryStream memoryStream = new MemoryStream(data);
            AlbumArt = new Bitmap(memoryStream);
        }
    }
    
    private void _OnStreamUpdated()
    {
        Dispatcher.UIThread.Post(_OnStreamPropertiesUpdated);
    }

    [RelayCommand]
    public void PreviousClicked()
    {
        m_Stream?.CLIENT_SendControlCommand(Stream.EControlCommand.previous);
    }
    
    [RelayCommand]
    public void NextClicked()
    {
        m_Stream?.CLIENT_SendControlCommand(Stream.EControlCommand.next);
    }
    
    [RelayCommand]
    public void PlayClicked()
    {
        m_Stream?.CLIENT_SendControlCommand(Stream.EControlCommand.play);
    }
    
    [RelayCommand]
    public void PauseClicked()
    {
        m_Stream?.CLIENT_SendControlCommand(Stream.EControlCommand.pause);
    }
    
    [RelayCommand]
    public void PlayPauseClicked()
    {
        m_Stream?.CLIENT_SendControlCommand(Stream.EControlCommand.playPause);
    }
#if DEBUG
    public StreamViewModel() : this(XamlPreviewHelpers.GetStream(0))
    {
    }
#endif
}