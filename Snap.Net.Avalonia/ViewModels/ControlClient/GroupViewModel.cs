using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Helpers;
using Snap.Net.Avalonia.Views;
using Snap.Net.ControlClient.JsonRpcData;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class GroupViewModel : VolumeControlViewModel
{
    private IServiceProvider m_ServiceProvider;
    private IControlClientService m_ControlClientService;
    private ISettingsService m_SettingsService;
    private Group m_Group;
    
    [ObservableProperty]
    private StreamViewModel? m_StreamView;
    
    public ObservableCollection<ControlClientViewModel> ControlClients { get; } = new ObservableCollection<ControlClientViewModel>();
    
    public Group Group => m_Group;
    
    public GroupViewModel(IServiceProvider serviceProvider
        , IControlClientService controlClientService
        , ISettingsService settingsService
        , Group group)
    {
        m_ServiceProvider = serviceProvider;
        m_SettingsService = settingsService;
        m_ControlClientService = controlClientService;
        m_Group = group;
        
        _RefreshData();
        m_Group.CLIENT_OnVolumeUpdated += _OnVolumeUpdated;
        m_Group.SERVER_OnGroupUpdated += _OnGroupUpdated;
    }

    private void _RefreshData()
    {
        ServerData? serverData = m_ControlClientService.GetServerData();
        if (serverData == null)
        {
            return;
        }
        Name = m_Group.Name;
        Volume = m_Group.VolumePercent;
        Stream? stream = serverData.streams.SingleOrDefault(s => s.id == m_Group.stream_id);
        if (stream != null)
        {
            StreamView = ActivatorUtilities.CreateInstance<StreamViewModel>(m_ServiceProvider, stream);    
        }
        Muted = m_Group.muted;
        _UpdateClientList();
    }
    
    private void _OnGroupUpdated()
    {
        Dispatcher.UIThread.Post(_RefreshData);
    }

    private void _UpdateClientList()
    {
        ControlClients.Clear();
        foreach (Client client in m_Group.clients)
        {
            if (client.connected || m_SettingsService.Get<bool>(SettingsKeys.SHOW_DISCONNECTED_CLIENTS))
            {
                ControlClients.Add(ActivatorUtilities.CreateInstance<ControlClientViewModel>(m_ServiceProvider, client));    
            }
        }
    }
    
    private void _OnVolumeUpdated()
    {
        SetProperty(ref m_Volume, m_Group.VolumePercent);
        OnPropertyChanged(nameof(Volume));
    }

    protected override void _OnVolumeChanged(int value)
    {
        m_Group.CLIENT_SetVolume((int)value);
    }
    
    public override void MuteToggled()
    {
        m_Group.CLIENT_ToggleMuted();
    }

    [RelayCommand]
    public void OnGroupClicked()
    {
        EditGroupWindow editGroupWindow = ActivatorUtilities.CreateInstance<EditGroupWindow>(m_ServiceProvider);
        editGroupWindow.DataContext = ActivatorUtilities.CreateInstance<EditGroupWindowViewModel>(m_ServiceProvider, this);
        editGroupWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        editGroupWindow.Show();
    }
    
    [RelayCommand]
    public void OnStreamClicked()
    {
        StreamWindow streamWindow = ActivatorUtilities.CreateInstance<StreamWindow>(m_ServiceProvider);
        streamWindow.DataContext = ActivatorUtilities.CreateInstance<StreamWindowViewModel>(m_ServiceProvider, StreamView!);
        streamWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        streamWindow.Show();
    }
    
#if DEBUG
    public GroupViewModel(Group group, Stream stream)
    {
        m_Group = group;
        Name = m_Group.Name;
        Volume = m_Group.VolumePercent;
        StreamView = new StreamViewModel(stream);
        Muted = m_Group.muted;
        
        ControlClients.Clear();
        foreach (Client client in m_Group.clients)
        {
            ControlClients.Add(new ControlClientViewModel(client));
        }
    }

    public GroupViewModel() : this(XamlPreviewHelpers.GetGroup(0, 4, 0), XamlPreviewHelpers.GetStream(0))
    {
        
    }
    
#endif    
}