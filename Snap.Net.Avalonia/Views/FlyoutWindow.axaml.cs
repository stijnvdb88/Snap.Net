using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Styling;
using Snap.Net.Avalonia.Enums;
using Snap.Net.Avalonia.Helpers;
using Snap.Net.Avalonia.ViewModels;
using ICloseable = Snap.Net.Avalonia.Contracts.ICloseable;

namespace Snap.Net.Avalonia.Views;

public partial class FlyoutWindow : Window, ICloseable
{
    private EPanelPosition m_PanelPosition;
    
#if DEBUG
    public FlyoutWindow()
    {
        m_PanelPosition =  EPanelPosition.BottomRight;
        CanResize = false;
        ShowInTaskbar = false;
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Height = 320;
        _Reposition();
#if DEBUG        
        Application.Current.RequestedThemeVariant = XamlPreviewHelpers.ThemeVariant;
#endif                
        InitializeComponent();
    }
#endif    
    
    public FlyoutWindow(EPanelPosition panelPosition, double height)
    {
        m_PanelPosition = panelPosition;
        CanResize = false;
        ShowInTaskbar = false;
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Height = height;
        _Reposition();
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _Reposition();
    }

    protected override void OnResized(WindowResizedEventArgs e)
    {
        _Reposition((int)e.ClientSize.Width);
    }

    private void _Reposition(int newWindowWidth = 0)
    {
        Screen screen = Screens.Primary!;
        double scaling = screen.Scaling; 

        if (newWindowWidth == 0)
        {
            newWindowWidth = (int)Bounds.Width;
        }

        int physicalWidth  = (int)(newWindowWidth * scaling);
        int physicalHeight = (int)(Height * scaling);

        PixelPoint panelPosition = m_PanelPosition == EPanelPosition.BottomRight
            ? screen.WorkingArea.BottomRight
            : screen.WorkingArea.TopRight;

        int minY = m_PanelPosition == EPanelPosition.BottomRight ? physicalHeight : 0;

        Position = panelPosition - new PixelVector(physicalWidth, minY);
    }
}