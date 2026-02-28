using Avalonia;
using Avalonia.Controls;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Helpers;

namespace Snap.Net.Avalonia.Views;

public partial class SettingsWindow : Window, ICloseable
{
    public SettingsWindow()
    {
#if DEBUG        
        Application.Current.RequestedThemeVariant = XamlPreviewHelpers.ThemeVariant;
#endif                
        InitializeComponent();
    }
}