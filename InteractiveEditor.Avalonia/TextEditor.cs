using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace InteractiveEditor.Avalonia;

// A text box: for Text, and for a Number the spinner does not take (P7.9). It writes on Enter and when it
// loses the focus, and Esc goes back to what the view holds (P2.12); the core reads the text in the
// inspector's culture. A text that does not convert stays as it was typed, with the failure on the row;
// the object keeps its value. The placeholder tells null from an empty text, and shows a grey dash when
// the objects hold different values (P7.19).
internal sealed class TextEditor : AvaloniaEditor
{
    private readonly TextBox Box = new();

    // The text of the value shown last, and the text the last write refused.
    private string Shown = string.Empty;
    private string? Refused;

    public TextEditor(AvaloniaRow row) : base(row)
    {
        Box.KeyDown += OnKeyDown;
        Box.LostFocus += OnLostFocus;
    }

    public override Control Control => Box;

    // While the box holds an edit of its own, a new value waits for Enter or Esc, so typing is not lost.
    public override void ShowValue()
    {
        Show(force: false);
    }

    // A read-only text can still be selected and copied.
    public override void ShowState(bool readOnly, bool enabled)
    {
        Box.IsReadOnly = readOnly;
        Box.IsEnabled = enabled;
    }

    // Objects that hold different values show none, and what is typed goes to all of them.
    private void Show(bool force)
    {
        if (!force && Box.IsKeyboardFocusWithin && Box.Text != Shown)
            return;

        // A number that scrubs shows the first object's value, as P2.4 has it; the label stays italic.
        var mixed = Row.Mixed && !Row.Scrubs;
        var value = Node.ViewValue;

        Shown = mixed ? string.Empty : Format(value);
        Box.Text = Shown;
        Box.PlaceholderText = mixed ? "—" : value == null ? "null" : null;
        Refused = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
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

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Commit();
    }

    // A write that goes through shows the value as the node now holds it (a range or a rule may have
    // changed it); one that fails leaves the text, and is not tried again until the text changes.
    private void Commit()
    {
        var typed = Box.Text ?? string.Empty;

        if (Box.IsReadOnly || typed == Shown || typed == Refused)
            return;

        if (Row.Write(() => Node.SetValue(typed)) && Node.Failure == null)
            Show(force: true);
        else
            Refused = typed;
    }
}
