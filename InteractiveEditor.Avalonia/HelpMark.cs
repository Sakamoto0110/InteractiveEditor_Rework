using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace InteractiveEditor.Avalonia;

// The help mark of a row, (?) (P7.15): on one line, in the theme's color for secondary text, but enabled,
// so it takes the click, with the hand cursor. It takes the press too, so in a group's header the click
// opens the help instead of the group.
internal sealed class HelpMark : Border
{
    public HelpMark()
    {
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        var text = new TextBlock
        {
            Text = "(?)",
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        text.Bind(TextBlock.ForegroundProperty, text.GetResourceObservable("SystemControlForegroundBaseMediumBrush"));
        Child = text;

        AutomationProperties.SetName(this, "Help");
    }

    public event EventHandler? Clicked;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        e.Handled = true;
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}
