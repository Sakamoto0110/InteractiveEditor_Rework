using System.Drawing;
using System.Windows.Forms;

namespace InteractiveEditor.Views.WinForms;

// The long help of a node (P7.15): a modal window, titled with the row's label, that blocks the one
// behind it until it closes. The text scrolls and can be selected; OK, Enter and Esc close it.
internal sealed class HelpDialog : Form
{
    public HelpDialog(string title, string text)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 220);
        MinimumSize = new Size(240, 160);
        Padding = new Padding(8, 8, 8, 0);

        var body = new TextBox
        {
            Name = "text",
            Multiline = true,
            ReadOnly = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Text = text.ReplaceLineEndings("\r\n"),
        };

        var ok = new Button { Name = "ok", Text = "OK", DialogResult = DialogResult.OK };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8),
        };

        buttons.Controls.Add(ok);
        Controls.AddRange([body, buttons]);

        // The text fills what the buttons leave, so it is docked last.
        body.BringToFront();

        AcceptButton = ok;
        CancelButton = ok;
        ActiveControl = ok;
    }
}
