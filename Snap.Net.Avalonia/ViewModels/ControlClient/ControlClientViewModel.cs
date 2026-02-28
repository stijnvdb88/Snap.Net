using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Helpers;
using Snap.Net.Avalonia.Views;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.ViewModels.ControlClient;

public partial class ControlClientViewModel : VolumeControlViewModel
{
    private Client m_Client;
    private IServiceProvider m_ServiceProvider;

    [ObservableProperty]
    private int m_Latency;
    
    [ObservableProperty]
    private string? m_Mac;
    
    [ObservableProperty]
    private string? m_Id;
    
    [ObservableProperty]
    private string? m_Ip;
    
    [ObservableProperty]
    private string? m_Host;
    
    [ObservableProperty]
    private string? m_OS;
    
    [ObservableProperty]
    private string? m_Architecture;
    
    [ObservableProperty]
    private string? m_Version;

    [ObservableProperty] 
    private string? m_LastSeen;

    [ObservableProperty]
    private TextDecorationCollection? m_ClientNameTextDecorations;
    
    public Client Client => m_Client;
    
    public ControlClientViewModel(IServiceProvider serviceProvider, Client client)
    {
        m_ServiceProvider = serviceProvider;
        m_Client = client;
        _RefreshClientData();
        m_Client.SERVER_OnClientUpdated += () =>
        {
            Dispatcher.UIThread.Post(_RefreshClientData);
        };
    }

    private void _RefreshClientData()
    {
        Name = m_Client.Name;
        m_Volume = m_Client.config.volume.percent;
        m_Client.config.SERVER_OnVolumeUpdated += _OnVolumeUpdated;
        Muted = m_Client.config.volume.muted;
        Latency = m_Client.config.latency;
        Mac = m_Client.host.mac;
        Id = m_Client.id;
        Ip = m_Client.host.ip;
        Host = m_Client.host.name;
        OS = m_Client.host.os;
        Architecture = m_Client.host.arch;
        Version = m_Client.snapclient.version;
        ClientNameTextDecorations = m_Client.connected ? null : TextDecorations.Strikethrough;
        if (m_Client.connected)
        {
            LastSeen = "Online";
        }
        else
        {
            LastSeen = m_Client.lastSeen.GetDateTime().ToString(CultureInfo.CurrentCulture);
        }
    }
    
    private void _OnVolumeUpdated()
    {
        SetProperty(ref m_Volume, m_Client.config.volume.percent);
        OnPropertyChanged(nameof(Volume));
    }

    protected override void _OnVolumeChanged(int value)
    {
        m_Client.config.volume.CLIENT_SetPercent((int)value);
    }

    public override void MuteToggled()
    {
        m_Client.config.volume.CLIENT_ToggleMuted();
    }
    
    [RelayCommand]
    public void OnClientClicked()
    {
        EditControlClientWindow editControlClientWindow = ActivatorUtilities.CreateInstance<EditControlClientWindow>(m_ServiceProvider);
        editControlClientWindow.DataContext = ActivatorUtilities.CreateInstance<EditControlClientWindowViewModel>(m_ServiceProvider, this);
        editControlClientWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        editControlClientWindow.Show();
    }
    
#if DEBUG
    public ControlClientViewModel(Client client)
    {
        m_Client = client;
        _RefreshClientData();
    }

    public ControlClientViewModel() : this(XamlPreviewHelpers.GetClient(0))
    {
    }
#endif   
}