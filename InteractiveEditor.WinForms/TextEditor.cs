using System.Windows.Forms;

namespace InteractiveEditor.WinForms;

// A text box: for Text and Number, and read-only for Display (P7.9). It writes on Enter and when it
// loses the focus, and Esc goes back to what the view holds (P2.12). A text that does not convert stays
// as it was typed, with the failure on the row; the object keeps its value.
internal sealed class TextEditor : WinFormsEditor
{
    private readonly TextBox Box = new();
    private readonly bool Display;

    // The text of the value shown last, and the text the last write refused.
    private string Shown = string.Empty;
    private string? Refused;

    public TextEditor(WinFormsRow row, bool display) : base(row)
    {
        Display = display;
        Box.ReadOnly = display;

        if (display)
            return;

        // Enter and Esc reach the box even in a form with an accept or a cancel button.
        Box.PreviewKeyDown += (_, e) => e.IsInputKey |= e.KeyCode is Keys.Enter or Keys.Escape;
        Box.KeyDown += OnKeyDown;
        Box.Leave += (_, _) => Commit();
    }

    public override Control Control => Box;

    // While the box holds an edit of its own, a new value waits for Enter or Esc, so typing is not lost.
    public override void ShowValue()
    {
        Show(force: false);
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Box.ReadOnly = Display || readOnly;
        Box.Enabled = enabled;

        if (failed)
            Box.BackColor = FailedColor;
        else
            Box.ResetBackColor();
    }

    // Objects that hold different values show none, with a grey dash, and what is typed goes to all of
    // them; a row that scrubs shows the first one's instead, since the drag moves each from its own
    // (P2.4, P7.19).
    private void Show(bool force)
    {
        if (!force && Box.Focused && Box.Text != Shown)
            return;

        var mixed = Row.Mixed && !Row.Scrubs;
        Shown = mixed ? string.Empty : Format(Node.ViewValue);
        Box.PlaceholderText = mixed ? "\u2014" : string.Empty;
        Box.Text = Shown;
        Refused = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            Commit();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Show(force: true);
            e.SuppressKeyPress = true;
        }
    }

    // A write that goes through shows the value as the node now holds it (7,6 in an int shows 8); one
    // that fails leaves the text, and is not tried again until the text changes.
    private void Commit()
    {
        var typed = Box.Text;

        if (Box.ReadOnly || typed == Shown || typed == Refused)
            return;

        if (Row.Write(() => Node.SetValue(typed)) && Node.Failure == null)
            Show(force: true);
        else
            Refused = typed;
    }
}
