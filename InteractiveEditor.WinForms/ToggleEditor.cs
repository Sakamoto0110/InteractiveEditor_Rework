using System.Drawing;
using System.Windows.Forms;

namespace InteractiveEditor.WinForms;

// A check box for a bool (P7.9), written at once (P2.12).
internal sealed class ToggleEditor : WinFormsEditor
{
    private readonly CheckBox Box = new();
    private bool Showing;

    public ToggleEditor(WinFormsRow row) : base(row)
    {
        Box.CheckedChanged += OnCheckedChanged;
    }

    public override Control Control => Box;

    // The box lets the row's background through, so the row under the mouse shows behind it too.
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Box.Enabled = enabled && !readOnly;
        Box.BackColor = failed ? FailedColor : Color.Transparent;
    }

    // Objects that hold different values leave the box indeterminate (P7.19); a click sets all of them.
    public override void ShowValue()
    {
        Showing = true;
        Box.CheckState = Row.Mixed ? CheckState.Indeterminate : Node.ViewValue is true ? CheckState.Checked : CheckState.Unchecked;
        Showing = false;
    }

    private void OnCheckedChanged(object? sender, EventArgs e)
    {
        if (Showing)
            return;

        var value = Box.CheckState == CheckState.Checked;
        Row.Write(() => Node.SetValue(value));
        Row.Show();
    }
}
