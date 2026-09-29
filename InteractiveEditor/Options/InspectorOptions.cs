using System.Globalization;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.Options;

// The options of one inspector, between the global ones and those of each node.
public sealed class InspectorOptions
{
    // How text typed in the view is read into numbers, dates and the rest. Null means the current
    // culture at the time of each conversion.
    public CultureInfo? Culture { get; set; }

    // Which way values move on their own between the view and the objects.
    public BinderControlMode BinderControl { get; set; } = BinderControlMode.Automatic;

    // The layout of the rows (P7.5), in the view's units: pixels in WinForms, device-independent
    // units in WPF. A length that is not a number from zero up is a mistake, and throws.

    // The height of a row: 23, the FieldHeight the OverlayApplication used.
    public double RowHeight
    {
        get;
        set => field = double.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(RowHeight), value, "The height of a row is more than zero.");
    } = 23;

    // The space between two rows, and between a group's row and the rows inside it.
    public double RowSpacing
    {
        get;
        set => field = Length(value, nameof(RowSpacing));
    } = 2;

    // How far the rows inside a group go in, at each level.
    public double Indent
    {
        get;
        set => field = Length(value, nameof(Indent));
    } = 16;

    // The width of the labels at the top level; deeper ones lose the indent, so that the editors line
    // up in one column.
    public double LabelWidth
    {
        get;
        set => field = Length(value, nameof(LabelWidth));
    } = 120;

    // The space between a label and its editor.
    public double LabelSpacing
    {
        get;
        set => field = Length(value, nameof(LabelSpacing));
    } = 4;

    // The space around all the rows.
    public PxPadding Padding
    {
        get;
        set
        {
            Length(value.Left, nameof(Padding));
            Length(value.Top, nameof(Padding));
            Length(value.Right, nameof(Padding));
            Length(value.Bottom, nameof(Padding));
            field = value;
        }
    } = new(4);

    // The item lines a list editor shows before it scrolls; it has one more line, for its buttons.
    public int ListRows
    {
        get;
        set => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(ListRows), value, "A list editor shows at least one item line.");
    } = 5;

    internal CultureInfo CultureInUse => Culture ?? CultureInfo.CurrentCulture;

    internal bool ViewToInstance => BinderControl.HasFlag(BinderControlMode.ViewToInstance);

    internal bool InstanceToView => BinderControl.HasFlag(BinderControlMode.InstanceToView);

    private static double Length(double value, string name)
    {
        return double.IsFinite(value) && value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(name, value, "A length is a number from zero up.");
    }
}
