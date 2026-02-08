using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Helpers;
using Snap.Net.Avalonia.Views;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class StreamWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private StreamViewModel m_StreamViewModel;

    [ObservableProperty]
    private string? m_Status;
    
    [ObservableProperty]
    private string? m_Uri;

    
    [ObservableProperty]
    private string? m_ChunkMs;
    
    [ObservableProperty]
    private string? m_Codec;
    
    [ObservableProperty]
    private string? m_SampleFormat;
    
    public StreamWindowViewModel(StreamViewModel streamViewModel)
    {
        StreamViewModel = streamViewModel;
        if (streamViewModel.Stream != null)
        {
            Status = streamViewModel.Stream.status;
            Uri = $"{streamViewModel.Stream.uri.scheme}://{streamViewModel.Stream.uri.path}";
            ChunkMs = streamViewModel.Stream.uri.query.chunk_ms;
            Codec = streamViewModel.Stream.uri.query.codec;
            SampleFormat = streamViewModel.Stream.uri.query.sampleformat;
        }
    }

    [RelayCommand]
    private void Close(ICloseable closeable)
    {
        closeable.Close();
    }
    
#if DEBUG
    public StreamWindowViewModel() : this(new StreamViewModel(XamlPreviewHelpers.GetStream(0)))
    {
    }
#endif
}
