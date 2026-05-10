using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace Snap.Net.Avalonia.Behaviors
{
    public class CloseFlyoutAction : AvaloniaObject, IAction
    {
        public object Execute(object sender, object parameter)
        {
            if (sender is Control control)
            {
                StyledElement? parent = control.Parent;
                while (parent != null)
                {
                    if (parent is Button button && button.Flyout != null)
                    {
                        button.Flyout.Hide();
                        return null!;
                    }
                    parent = (parent as Visual)?.Parent;
                }
            }
            return null!;
        }
    }
}