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

    [ObservableProperty]
    private bool m_AutoStart;

    [ObservableProperty] 
    private bool m_IsBroadcasting;
    
    [ObservableProperty]
    private AudioDeviceViewModel? m_SelectedAudioDeviceViewModel;
    
    public ObservableCollection<AudioDeviceViewModel> AudioDeviceViewModels { get; } =
        new ObservableCollection<AudioDeviceViewModel>();

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

        IEnumerable<IAudioDevice> audioDevices = m_BroadcastService.GetAudioDevices(EDeviceType.All);
        foreach (IAudioDevice audioDevice in audioDevices)
        {
            AudioDeviceViewModel audioDeviceViewModel =
                ActivatorUtilities.CreateInstance<AudioDeviceViewModel>(m_ServiceProvider, audioDevice);
            AudioDeviceViewModels.Add(audioDeviceViewModel);
            if (audioDevice.Id == selectedId)
            {
                SelectedAudioDeviceViewModel = audioDeviceViewModel;
            }
        }
    }

    [RelayCommand]
    public void Save(ICloseable closeable)
    {
        m_SettingsService.Set(SettingsKeys.BROADCAST_DEVICE_ID, SelectedAudioDeviceViewModel?.Id);
        m_SettingsService.Set(SettingsKeys.BROADCAST_PORT, Port);
        m_SettingsService.Set(SettingsKeys.BROADCAST_AUTO_START, AutoStart);
        closeable.Close();
    }
}