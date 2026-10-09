using System.Windows.Forms;

namespace InteractiveEditor.WinForms;

// A button with the Text of a ButtonNode, which calls Press() (P7.9). An action that throws is a failure
// on the row, as the core reports it (P1.6).
internal sealed class ButtonEditor : WinFormsEditor
{
    private readonly Button Push = new() { UseMnemonic = false };

    public ButtonEditor(WinFormsRow row) : base(row)
    {
        Push.Click += OnClick;
    }

    public override Control Control => Push;

    public override void ShowValue()
    {
        Push.Text = Node is ButtonNode button ? button.Text : Node.Label;
    }

    // Only a ButtonNode has an action to run.
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Push.Enabled = enabled && !readOnly && Node is ButtonNode;

        if (failed)
        {
            Push.BackColor = FailedColor;
        }
        else
        {
            Push.ResetBackColor();
            Push.UseVisualStyleBackColor = true;
        }
    }

    private void OnClick(object? sender, EventArgs e)
    {
        if (Node is ButtonNode button)
            Row.Write(button.Press);
    }
}
