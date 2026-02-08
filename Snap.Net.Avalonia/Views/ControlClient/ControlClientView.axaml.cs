using Avalonia;
using Avalonia.Controls;
using Snap.Net.Avalonia.Helpers;

namespace Snap.Net.Avalonia.Views;

public partial class ControlClientView : UserControl
{
    public ControlClientView()
    {
#if DEBUG        
        Application.Current.RequestedThemeVariant = XamlPreviewHelpers.ThemeVariant;
#endif        
        InitializeComponent();
    }
}