using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
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

    [ObservableProperty]
    private bool m_AutoStart;

    [ObservableProperty] 
    private bool m_IsBroadcastConnected;
    
    [ObservableProperty]
    private BroadcastAudioDeviceViewModel? m_SelectedAudioDeviceViewModel;
    
    public ObservableCollection<BroadcastAudioDeviceViewModel> AudioDeviceViewModels { get; } =
        new ObservableCollection<BroadcastAudioDeviceViewModel>();

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
        AutoStart = m_SettingsService.Get<bool>(SettingsKeys.BROADCAST_AUTO_START, false);
        string? selectedId = m_SettingsService.Get<string>(SettingsKeys.BROADCAST_DEVICE_ID, string.Empty);
        IsBroadcastConnected = m_BroadcastService.IsConnected;
        m_BroadcastService.OnIsConnectedChanged += OnBroadcastConnectionStateChanged;

        IEnumerable<IAudioDevice> audioDevices = m_BroadcastService.GetAudioDevices(EDeviceType.All);
        foreach (IAudioDevice audioDevice in audioDevices)
        {
            BroadcastAudioDeviceViewModel broadcastAudioDeviceViewModel =
                ActivatorUtilities.CreateInstance<BroadcastAudioDeviceViewModel>(m_ServiceProvider, audioDevice);
            AudioDeviceViewModels.Add(broadcastAudioDeviceViewModel);
            if (audioDevice.Id == selectedId)
            {
                SelectedAudioDeviceViewModel = broadcastAudioDeviceViewModel;
            }
        }
    }

    [RelayCommand]
    private async Task ToggleBroadcastAsync()
    {
        if (IsBroadcastConnected)
        {
            m_BroadcastService.StopBroadcast();
        }
        else
        {
            _Save();
            string? host = m_SettingsService.Get<string>(SettingsKeys.HOST);
            if (string.IsNullOrEmpty(host) == false && Port != null && SelectedAudioDeviceViewModel != null)
            {
                int port = (int)Port;
                await m_BroadcastService.StartBroadcast(host, port, SelectedAudioDeviceViewModel.Id);
            }
        }
    }

    private void OnBroadcastConnectionStateChanged(bool connected)
    {
        Dispatcher.UIThread.Post(() => IsBroadcastConnected = connected);
    }

    [RelayCommand]
    public void Save(ICloseable closeable)
    {
        _Save();
        closeable.Close();
    }

    private void _Save()
    {
        m_SettingsService.Set(SettingsKeys.BROADCAST_DEVICE_ID, SelectedAudioDeviceViewModel?.Id);
        m_SettingsService.Set(SettingsKeys.BROADCAST_PORT, Port);
        m_SettingsService.Set(SettingsKeys.BROADCAST_AUTO_START, AutoStart);
    }
}