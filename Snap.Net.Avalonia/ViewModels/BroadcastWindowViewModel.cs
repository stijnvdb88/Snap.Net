using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Broadcast;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Enums;
using Snap.Net.Avalonia.ViewModels.Broadcast;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.ViewModels;

public partial class BroadcastWindowViewModel : ViewModelBase
{
    private IServiceProvider m_ServiceProvider;
    private ISettingsService m_SettingsService;
    private IBroadcastService m_BroadcastService;
    
    [ObservableProperty]
    private int? m_Port;

    public ObservableCollection<AudioDeviceViewModel> AudioDeviceViewModels { get; } =
        new ObservableCollection<AudioDeviceViewModel>();
    
    [ObservableProperty]
    private AudioDeviceViewModel? m_SelectedAudioDeviceViewModel;

#if DEBUG
    public BroadcastWindowViewModel()
    {
        Port = 4953;
    }
#endif    
    
    public BroadcastWindowViewModel(IServiceProvider serviceProvider, ISettingsService settingsService,  IBroadcastService broadcastService)
    {
        m_ServiceProvider = serviceProvider;
        m_SettingsService = settingsService;
        m_BroadcastService = broadcastService;
        Port = m_SettingsService.Get<int>(SettingsKeys.BROADCAST_PORT, 4953);

        IEnumerable<IAudioDevice> audioDevices = m_BroadcastService.GetAudioDevices(EDeviceType.All);
        foreach (IAudioDevice audioDevice in audioDevices)
        {
            AudioDeviceViewModels.Add(ActivatorUtilities.CreateInstance<AudioDeviceViewModel>(m_ServiceProvider, audioDevice));
        }
    }

    [RelayCommand]
    public void Save(ICloseable closeable)
    {
        // m_SettingsService.Set(SettingsKeys.HOST, Host);
        m_SettingsService.Set(SettingsKeys.BROADCAST_PORT, Port);
        // m_SettingsService.Set(SettingsKeys.SHOW_DISCONNECTED_CLIENTS, ShowDisconnectedClients);
        // m_SettingsService.Set(SettingsKeys.PANEL_POSITION, PanelPosition);
        // if (string.IsNullOrEmpty(Host) == false && Port != null)
        // {
        //     m_ControlClientService.InitializeAsync(Host, (int)Port).ConfigureAwait(false);    
        // }
        closeable.Close();
    }
}