using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
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
    private IBroadcastService m_BroadcastService;
    private FlyoutWindow? m_FlyoutWindow = null;
    private SettingsWindow? m_SettingsWindow = null;
    private BroadcastWindow? m_BroadcastWindow = null;
    private PlayerWindow? m_PlayerWindow = null;
    
    private static readonly WindowIcon s_DefaultIcon = new WindowIcon(
        AssetLoader.Open(new System.Uri("avares://Snap.Net.Avalonia/Assets/snapcast.ico")));
    private static readonly WindowIcon s_BroadcastingIcon = new WindowIcon(
        AssetLoader.Open(new System.Uri("avares://Snap.Net.Avalonia/Assets/snapcast_r.ico")));

    
    
    [ObservableProperty]
    private bool m_AddOpenFlyoutEntry = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

    [ObservableProperty]
    private string m_OpenFlyoutEntryLabel = "Open";
    
    public event Action<bool>? OnTrayIconChanged;

    private Dictionary<Type, Window> m_OpenWindows = new Dictionary<Type, Window>();
    
    public AppViewModel(
        IServiceProvider serviceProvider
        , IControlClientService controlClientService
        , IBroadcastService broadcastService
        , ISettingsService settingsService)
    {
        m_ServiceProvider = serviceProvider;
        m_ControlClientService = controlClientService;
        m_SettingsService = settingsService;
        m_BroadcastService = broadcastService;
        m_BroadcastService.OnIsBroadcastingChanged += _OnBroadcastingChanged;
        m_BroadcastService.OnIsConnectedChanged += OnBroadcastConnectedChanged;
    }

    private void OnBroadcastConnectedChanged(bool connected)
    {
        if (connected == false)
        {
            OnTrayIconChanged?.Invoke(false);
        }
    }

    private void _OnBroadcastingChanged(bool broadcasting)
    {
        OnTrayIconChanged?.Invoke(broadcasting);
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
            EPanelPosition defaultPanelPosition = OperatingSystem.IsMacOS() ? EPanelPosition.TopRight : EPanelPosition.BottomRight;
            EPanelPosition panelPosition = m_SettingsService.Get<EPanelPosition>(SettingsKeys.PANEL_POSITION, defaultPanelPosition);
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
        _ShowWindow<SettingsWindow, SettingsWindowViewModel>();
    }
    
    [RelayCommand]
    private void ShowPlayer()
    {
        _ShowWindow<PlayerWindow, PlayerWindowViewModel>();
    }

    [RelayCommand]
    private void ShowBroadcastWindow()
    {
        _ShowWindow<BroadcastWindow, BroadcastWindowViewModel>();
    }
    
    private void _ShowWindow<TWindowType, TViewModelType>() where TWindowType : Window, new() where TViewModelType : ViewModelBase  
    {
        Window? existing = null;
        if (m_OpenWindows.ContainsKey(typeof(TWindowType)))
        {
            existing = m_OpenWindows[typeof(TWindowType)];
        }

        if (existing != null)
        {
            if (existing.WindowState == WindowState.Minimized)
            {
                existing.WindowState = WindowState.Normal;
            }
            existing.Activate();
            return;
        }

        TWindowType window = new TWindowType();
        m_OpenWindows.Add(typeof(TWindowType), window);
        window.Closed += (_, _) => m_OpenWindows.Remove(typeof(TWindowType));
        window.DataContext = m_ServiceProvider.GetService(typeof(TViewModelType));
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.Show();
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