using System.Globalization;
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

        var value = Node.ViewValue is IConvertible convertible ? convertible.ToDouble(CultureInfo.InvariantCulture) : Range.Min;
        var span = Range.Max - Range.Min;
        Bar.Value = span > 0 ? Math.Clamp((int)Math.Round((value - Range.Min) / span * Bar.Maximum), 0, Bar.Maximum) : 0;
    }

    private void OnScroll(object? sender, EventArgs e)
    {
        var value = Range.Min + (Range.Max - Range.Min) * Bar.Value / Bar.Maximum;
        Row.Write(() => Node.SetValue(value));
    }
}
