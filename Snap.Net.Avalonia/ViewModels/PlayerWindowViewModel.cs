using System;

using Snap.Net.Avalonia.Contracts.Services;

namespace Snap.Net.Avalonia.ViewModels;

public partial class PlayerWindowViewModel : ViewModelBase
{
    private IServiceProvider m_ServiceProvider;
    private ISettingsService m_SettingsService;

    public PlayerWindowViewModel(IServiceProvider serviceProvider, ISettingsService settingsService)
    {
        m_ServiceProvider = serviceProvider;
        m_SettingsService = settingsService;
    }
}