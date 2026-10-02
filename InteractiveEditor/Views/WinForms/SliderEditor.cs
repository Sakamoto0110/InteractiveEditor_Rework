using System.Windows.Forms;
using InteractiveEditor.Options;

namespace InteractiveEditor.Views.WinForms;

// A track bar over the node's range (P7.9). A track bar only counts in int, so its positions go by the
// step (a hundred with no step), and it writes while it is dragged (P2.12).
internal sealed class SliderEditor : WinFormsEditor
{
    private const int MaxPositions = 10000;

    private readonly TrackBar Bar = new() { AutoSize = false, TickStyle = TickStyle.None, Minimum = 0 };

    // The range the positions were made for.
    private NumericRange Range;

    // A track bar cannot let the row's background through, so it takes the color itself.
    private bool Failed;
    private bool Hovered;

    public SliderEditor(WinFormsRow row) : base(row)
    {
        Bar.Scroll += OnScroll;
    }

    public override Control Control => Bar;

    public override void ShowValue()
    {
        Range = Node.Range ?? default;
        Bar.Maximum = Range.Step > 0 ? Math.Clamp((int)Math.Round((Range.Max - Range.Min) / Range.Step), 1, MaxPositions) : 100;
        Bar.LargeChange = Math.Max(1, Bar.Maximum / 10);

        var value = ViewRules.ToDouble(Node.ViewValue, Range.Min);
        var span = Range.Max - Range.Min;
        Bar.Value = span > 0 ? Math.Clamp((int)Math.Round((value - Range.Min) / span * Bar.Maximum), 0, Bar.Maximum) : 0;
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Bar.Enabled = enabled && !readOnly;
        Failed = failed;
        Paint();
    }

    public override void ShowHover(bool hovered)
    {
        Hovered = hovered;
        Paint();
    }

    private void Paint()
    {
        if (Failed)
            Bar.BackColor = FailedColor;
        else if (Hovered)
            Bar.BackColor = WinFormsInspectorView.HoverColor;
        else
            Bar.ResetBackColor();
    }

    private void OnScroll(object? sender, EventArgs e)
    {
        var value = Range.Min + (Range.Max - Range.Min) * Bar.Value / Bar.Maximum;
        Row.Write(() => Node.SetValue(value));
    }
}
