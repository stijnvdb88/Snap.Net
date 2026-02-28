using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Helpers;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class EditGroupWindowViewModel : ViewModelBase
{
    private IControlClientService m_ControlClientService;
    
    [ObservableProperty]
    private GroupViewModel m_GroupView;

    [ObservableProperty]
    private StreamViewModel? m_SelectedStreamView;
    
    public ObservableCollection<GroupControlClientViewModel> ControlClients  { get; } = new ObservableCollection<GroupControlClientViewModel>();
    public ObservableCollection<StreamViewModel> Streams { get; } = new ObservableCollection<StreamViewModel>();
    
    public EditGroupWindowViewModel(IServiceProvider serviceProvider, IControlClientService controlClientService, GroupViewModel groupViewModel)
    {
        m_ControlClientService = controlClientService;
        GroupView = groupViewModel;
        
        ServerData? serverData = m_ControlClientService.GetServerData();
        if (serverData == null)
        {
            return;
        }
        Client[] clients = serverData.GetAllClients();
        foreach (Client client in clients)
        {
            ControlClients.Add(ActivatorUtilities.CreateInstance<GroupControlClientViewModel>(serviceProvider, client, m_GroupView.Group.HasClientWithId(client.id)));
        }

        Stream[] streams = serverData.streams;
        foreach (Stream stream in streams)
        {
            StreamViewModel streamViewModel =
                ActivatorUtilities.CreateInstance<StreamViewModel>(serviceProvider, stream);
            if (groupViewModel.StreamView?.Name == streamViewModel.Name)
            {
                SelectedStreamView = streamViewModel;
            }
            Streams.Add(streamViewModel);
        }
    }

    [RelayCommand]
    private void Save(ICloseable closeable)
    {
        GroupView.Group.CLIENT_SetClients(ControlClients.Where(c => c.InGroup).Select(c => c.Id).ToArray());
        GroupView.Group.CLIENT_SetName(GroupView.Name);
        GroupView.Group.CLIENT_SetStream(SelectedStreamView?.Name);
        
        closeable.Close();
    }
#if DEBUG
    public EditGroupWindowViewModel()
    {
        GroupView = new GroupViewModel();
        for (int i = 0; i < 4; i++)
        {
            ControlClients.Add(new GroupControlClientViewModel(XamlPreviewHelpers.GetClient(i), true));
            ControlClients.Add(new GroupControlClientViewModel(XamlPreviewHelpers.GetClient(i + 4), false));
        }

        StreamViewModel activeStream = new StreamViewModel(XamlPreviewHelpers.GetStream(0));
        SelectedStreamView = activeStream;
        Streams.Add(activeStream);
        Streams.Add(new StreamViewModel(XamlPreviewHelpers.GetStream(1)));
    }
#endif    
}
