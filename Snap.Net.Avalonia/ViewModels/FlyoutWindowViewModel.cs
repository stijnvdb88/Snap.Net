using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Helpers;
using Snap.Net.Avalonia.ViewModels.ControlClient;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.ViewModels;

public partial class FlyoutWindowViewModel : ViewModelBase
{
    private IControlClientService m_ControlClientService;
    private IServiceProvider m_ServiceProvider;
    private ISettingsService m_SettingsService;
    
    public ObservableCollection<GroupViewModel> Groups { get; private set; } = new ObservableCollection<GroupViewModel>();
    
    [ObservableProperty]
    private double m_FlyoutWidth = 600;
    
    [ObservableProperty]
    private double m_FlyoutHeight = 300;

    [ObservableProperty] 
    private bool m_ConnectionFailed;

    [ObservableProperty]
    private string m_ConnectionFailureMessage;
    
    private ServerData? m_ServerData;
    
    public FlyoutWindowViewModel(IServiceProvider serviceProvider,
        IControlClientService controlClientService,
        ISettingsService settingsService)
    {
        m_ServiceProvider = serviceProvider;
        m_ControlClientService = controlClientService;
        m_SettingsService = settingsService;
        
        m_ControlClientService.OnServerUpdated += _OnServerUpdated;
        m_ServerData = m_ControlClientService.GetServerData();
        _UpdateGroups();
        m_ConnectionFailed = m_ControlClientService.GetServerData() == null;
        string? host = settingsService.Get<string>(SettingsKeys.HOST);
        int port = settingsService.Get(SettingsKeys.PORT, 1705);
        m_ConnectionFailureMessage = $"Failed to connect to snapserver at '{host}:{port}'";
    }

    private void _OnServerUpdated()
    {
        m_ServerData = m_ControlClientService.GetServerData();
        Dispatcher.UIThread.Post(_UpdateGroups);
    }

    [RelayCommand]
    public void FlyoutRightClicked(ICloseable closeable)
    {
        closeable.Close();
    }

    private void _UpdateGroups()
    {
        Groups.Clear();
        if (m_ServerData == null)
        {
            return;
        }
        
        foreach (Group group in m_ServerData.groups)
        {
            if (group.clients.Any(c => c.connected) || m_SettingsService.Get<bool>(SettingsKeys.SHOW_DISCONNECTED_CLIENTS))
            {
                GroupViewModel groupViewModel = ActivatorUtilities.CreateInstance<GroupViewModel>(m_ServiceProvider, group);
                Groups.Add(groupViewModel);    
            }
        }
    }
    
#if DEBUG
    public FlyoutWindowViewModel()
    {
        int numGroups = 2;
        Group[] groups = new  Group[numGroups];
        Stream[] streams = new Stream[numGroups];
        Groups = new ObservableCollection<GroupViewModel>();
        for (int i = 0; i < numGroups; i++)
        {
            streams[i] = XamlPreviewHelpers.GetStream(i);
            groups[i] = XamlPreviewHelpers.GetGroup(i, 4, i);
            Groups.Add(new GroupViewModel(groups[i], streams[i]));
        }
    }
#endif    
}