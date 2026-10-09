using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InteractiveEditor.Wpf;

// A text box: for Text and Number, and read-only for Display (P7.9). It writes on Enter and when it
// loses the focus, and Esc goes back to what the view holds (P2.12). A text that does not convert stays
// as it was typed, with the failure on the row; the object keeps its value. A WPF text box has no
// placeholder, so the grey dash of mixed values is a text block over it, which lets the mouse through.
internal sealed class TextEditor : WpfEditor
{
    private readonly TextBox Box = new();
    private readonly TextBlock Dash = new()
    {
        Text = "—",
        Foreground = SystemColors.GrayTextBrush,
        IsHitTestVisible = false,
        VerticalAlignment = VerticalAlignment.Center,
        Visibility = Visibility.Collapsed,
    };

    private readonly bool Display;

    // The text of the value shown last, and the text the last write refused.
    private string Shown = string.Empty;
    private string? Refused;
    private bool Mixed;

    public TextEditor(WpfRow row, bool display) : base(row)
    {
        Display = display;
        Box.IsReadOnly = display;
        Box.TextChanged += (_, _) => ShowDash();

        if (display)
            return;

        // Before the window sees them, so a default or a cancel button does not take Enter or Esc.
        Box.PreviewKeyDown += OnKeyDown;
        Box.LostKeyboardFocus += (_, _) => Commit();
    }

    public override FrameworkElement Control => Box;

    public override IEnumerable<UIElement> Elements => [Box, Dash];

    public override void Place(Rect bounds)
    {
        base.Place(bounds);
        Canvas.SetLeft(Dash, bounds.X + 4);
        Canvas.SetTop(Dash, bounds.Y);
        Dash.Height = bounds.Height;
        Panel.SetZIndex(Dash, 1);
    }

    // While the box holds an edit of its own, a new value waits for Enter or Esc, so typing is not lost.
    public override void ShowValue()
    {
        Show(force: false);
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Box.IsReadOnly = Display || readOnly;
        Box.IsEnabled = enabled;

        if (failed)
            Box.Background = FailedBrush;
        else
            Box.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
    }

    // Objects that hold different values show none, with a grey dash, and what is typed goes to all of
    // them; a row that scrubs shows the first one's instead, since the drag moves each from its own
    // (P2.4, P7.19).
    private void Show(bool force)
    {
        if (!force && Box.IsKeyboardFocusWithin && Box.Text != Shown)
            return;

        Mixed = Row.Mixed && !Row.Scrubs;
        Shown = Mixed ? string.Empty : Format(Node.ViewValue);
        Box.Text = Shown;
        Refused = null;
        ShowDash();
    }

    private void ShowDash()
    {
        Dash.Visibility = Mixed && Box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Commit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Show(force: true);
            e.Handled = true;
        }
    }

    // A write that goes through shows the value as the node now holds it (7,6 in an int shows 8); one
    // that fails leaves the text, and is not tried again until the text changes.
    private void Commit()
    {
        var typed = Box.Text;

        if (Box.IsReadOnly || typed == Shown || typed == Refused)
            return;

        if (Row.Write(() => Node.SetValue(typed)) && Node.Failure == null)
            Show(force: true);
        else
            Refused = typed;
    }
}
