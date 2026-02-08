using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class GroupControlClientViewModel : ControlClientViewModel
{
    [ObservableProperty]
    private bool m_InGroup;
    
    public GroupControlClientViewModel(IServiceProvider serviceProvider, Client client, bool inGroup) : base(serviceProvider, client)
    {
        InGroup = inGroup;
        Id = client.id;
    }
    
#if DEBUG
    public GroupControlClientViewModel(Client client, bool inGroup) : base(client)
    {
        InGroup = inGroup;
        Id = client.id;
    }
#endif    
}