using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InteractiveEditor.Views.Wpf;

// The help mark of a row, (?) (P7.15): on one line, centred in its narrow column. In the color of a
// disabled control, but enabled, so it takes the click; it lets the row's background through.
internal sealed class HelpMark : Border
{
    public HelpMark()
    {
        Background = Brushes.Transparent;
        Cursor = Cursors.Hand;
        Child = new TextBlock
        {
            Text = "(?)",
            Foreground = SystemColors.GrayTextBrush,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        AutomationProperties.SetName(this, "Help");
        MouseLeftButtonUp += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Clicked;
}
