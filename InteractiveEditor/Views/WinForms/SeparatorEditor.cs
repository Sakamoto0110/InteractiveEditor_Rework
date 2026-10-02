using System.Drawing;
using System.Windows.Forms;

namespace InteractiveEditor.Views.WinForms;

// A horizontal line across the whole row (P7.9).
internal sealed class SeparatorEditor : WinFormsEditor
{
    private readonly Label Line = new() { AutoSize = false, BorderStyle = BorderStyle.Fixed3D };

    public SeparatorEditor(WinFormsRow row) : base(row)
    {
    }

    public override Control Control => Line;

    public override void Place(Rectangle bounds)
    {
        Line.Bounds = new Rectangle(bounds.X, bounds.Y + bounds.Height / 2 - 1, bounds.Width, 2);
    }

    public override void ShowValue()
    {
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
    }
}
