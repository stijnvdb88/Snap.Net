using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Contracts.Services;


namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class EditControlClientWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ControlClientViewModel m_ClientViewModel;

    [ObservableProperty] 
    private bool m_DeleteAvailable;
    
    public EditControlClientWindowViewModel(ControlClientViewModel clientViewModel)
    {
        ClientViewModel = clientViewModel;
        DeleteAvailable = ClientViewModel.Client.connected == false;
    }

    [RelayCommand]
    private void Save(ICloseable closeable)
    {
        ClientViewModel.Client.CLIENT_SetName(ClientViewModel.Name);
        ClientViewModel.Client.config.CLIENT_SetLatency(ClientViewModel.Latency);
        closeable.Close();
    }
    
    [RelayCommand]
    private void Remove(ICloseable closeable)
    {
        ClientViewModel.Client.CLIENT_Remove();
        closeable.Close();
    }

#if DEBUG
    public EditControlClientWindowViewModel() : this(new ControlClientViewModel())
    {
    }
#endif    
}
