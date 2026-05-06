using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DynamicData;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.ViewModels.Player;

namespace Snap.Net.Avalonia.ViewModels;

public partial class PlayerWindowViewModel : ViewModelBase
{
    private IServiceProvider m_ServiceProvider;
    private ISettingsService m_SettingsService;
    private IPlayerService m_PlayerService;

    public ObservableCollection<PlayerDeviceViewModel> PlayerDevices { get; } = new ObservableCollection<PlayerDeviceViewModel>();
    
    public PlayerWindowViewModel(IServiceProvider serviceProvider, ISettingsService settingsService, IPlayerService playerService)
    {
        m_ServiceProvider = serviceProvider;
        m_SettingsService = settingsService;
        m_PlayerService = playerService;
        
        Task.Run(_PopulatePlayerDevices).ConfigureAwait(false);
    }

    private async Task _PopulatePlayerDevices()
    {
        PlayerDevices.AddRange(await m_PlayerService.GetDevicesAsync());
    }
}