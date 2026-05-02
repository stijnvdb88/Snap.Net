using Avalonia;
using Avalonia.Controls;
using Snap.Net.Avalonia.Contracts;
using Snap.Net.Avalonia.Helpers;

namespace Snap.Net.Avalonia.Views;

public partial class BroadcastWindow : Window, ICloseable
{
    public BroadcastWindow()
    {
#if DEBUG        
        Application.Current.RequestedThemeVariant = XamlPreviewHelpers.ThemeVariant;
#endif                
        InitializeComponent();
    }
}