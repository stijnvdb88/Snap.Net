using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Snap.Net.Avalonia.Helpers;

namespace Snap.Net.Avalonia.Views;

public partial class GroupView : UserControl
{
    public GroupView()
    {
#if DEBUG        
        Application.Current.RequestedThemeVariant = XamlPreviewHelpers.ThemeVariant;
#endif        
        InitializeComponent();
    }
}