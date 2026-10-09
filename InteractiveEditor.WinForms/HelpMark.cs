using System.Drawing;
using System.Windows.Forms;

namespace InteractiveEditor.WinForms;

// The help mark of a row, (?) (P7.15). Drawn on one line, centred and with no padding, so it fits its
// narrow column: a label would add its padding and wrap the text. In the color of a disabled control,
// but enabled, so it takes the click; it lets the row's background through.
internal sealed class HelpMark : Control
{
    private const TextFormatFlags Flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding
        | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;

    public HelpMark()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);

        Text = "(?)";
        BackColor = Color.Transparent;
        ForeColor = SystemColors.GrayText;
        Cursor = Cursors.Hand;
        AccessibleName = "Help";
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, Flags);
    }
}
