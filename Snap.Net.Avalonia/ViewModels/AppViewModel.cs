using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Net.Avalonia.Consts;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Avalonia.Enums;
using Snap.Net.Avalonia.Views;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    private IServiceProvider m_ServiceProvider;
    private IControlClientService m_ControlClientService;
    private ISettingsService m_SettingsService;
    private FlyoutWindow? m_FlyoutWindow = null;
    private SettingsWindow? m_SettingsWindow = null;
    
    [ObservableProperty]
    private bool m_AddOpenFlyoutEntry = OperatingSystem.IsMacOS();

    [ObservableProperty]
    private string m_OpenFlyoutEntryLabel = "Open";
    
    public AppViewModel(
        IServiceProvider serviceProvider
        , IControlClientService controlClientService
        , ISettingsService settingsService)
    {
        m_ServiceProvider = serviceProvider;
        m_ControlClientService = controlClientService;
        m_SettingsService = settingsService;
    }

    [RelayCommand]
    public void ShowFlyout()
    {
        if (m_FlyoutWindow == null || m_FlyoutWindow.IsLoaded == false)
        {
            ServerData? serverData = m_ControlClientService.GetServerData();
            int flyoutHeight = 150;
            if (serverData != null)
            {
                flyoutHeight = 320;
            }
            EPanelPosition panelPosition = m_SettingsService.Get<EPanelPosition>(SettingsKeys.PANEL_POSITION, EPanelPosition.BottomRight);
            FlyoutWindowViewModel? viewModel = m_ServiceProvider.GetService<FlyoutWindowViewModel>();
            m_FlyoutWindow = new FlyoutWindow(panelPosition, flyoutHeight);
            m_FlyoutWindow.DataContext = viewModel;
            m_FlyoutWindow.Show();
            OpenFlyoutEntryLabel = "Close";
        }
        else
        {
            m_FlyoutWindow.Close();
            m_FlyoutWindow = null;
            OpenFlyoutEntryLabel = "Open";
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        m_SettingsWindow = new SettingsWindow();
        m_SettingsWindow.DataContext = m_ServiceProvider.GetService(typeof(SettingsWindowViewModel));
        m_SettingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        m_SettingsWindow.Show();
    }
    
    [RelayCommand]
    private void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}