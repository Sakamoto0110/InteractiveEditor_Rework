using System.Globalization;
using System.Runtime.CompilerServices;
using PixieLib;

namespace InteractiveEditor.Options;

// The options of one inspector, between the global ones and those of each node. A change in the
// culture or in the layout reaches the inspector's OptionChanged, with no node (P7.8), since a view
// shows values and rows by them; the binder control does not change what the view shows.
public sealed class InspectorOptions
{
    // How text typed in the view is read into numbers, dates and the rest. Null means the current
    // culture at the time of each conversion.
    public CultureInfo? Culture
    {
        get;
        set => Change(ref field, value);
    }

    // Which way values move on their own between the view and the objects.
    public BinderControlMode BinderControl { get; set; } = BinderControlMode.Automatic;

    // The layout of the rows (P7.5), in the view's units: pixels in WinForms, device-independent
    // units in WPF. A length that is not a number from zero up is a mistake, and throws.

    // The height of a row: 23, the FieldHeight the OverlayApplication used.
    public double RowHeight
    {
        get;
        set => Change(ref field, double.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(RowHeight), value, "The height of a row is more than zero."));
    } = 23;

    // The space between two rows, and between a group's row and the rows inside it.
    public double RowSpacing
    {
        get;
        set => Change(ref field, Length(value, nameof(RowSpacing)));
    } = 2;

    // How far the rows inside a group go in, at each level.
    public double Indent
    {
        get;
        set => Change(ref field, Length(value, nameof(Indent)));
    } = 16;

    // The width of the labels at the top level; deeper ones lose the indent, so that the editors line
    // up in one column.
    public double LabelWidth
    {
        get;
        set => Change(ref field, Length(value, nameof(LabelWidth)));
    } = 120;

    // The space between a label and its editor.
    public double LabelSpacing
    {
        get;
        set => Change(ref field, Length(value, nameof(LabelSpacing)));
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
            Change(ref field, value);
        }
    } = new(4);

    // The widest an editor gets (P7.14): in a wider row, what is left over goes to a space between the
    // label and the editor, and the editors line up on the right. Null lets the editor take the whole
    // rest of the row.
    public double? EditorMaxWidth
    {
        get;
        set => Change(ref field, value is { } width ? Length(width, nameof(EditorMaxWidth)) : null);
    } = 200;

    // The widest the rows get, the padding included (P7.14); a wider view leaves the rest empty, on the
    // right. Null, the default, takes the width the view gives.
    public double? MaxWidth
    {
        get;
        set => Change(ref field, value is { } width ? Length(width, nameof(MaxWidth)) : null);
    }

    // The width of the help mark, (?), which a node with Help has right before its editor (P7.15).
    public double HelpWidth
    {
        get;
        set => Change(ref field, Length(value, nameof(HelpWidth)));
    } = 16;

    // The item lines a list editor shows before it scrolls; it has one more line, for its buttons.
    public int ListRows
    {
        get;
        set => Change(ref field, value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(ListRows), value, "A list editor shows at least one item line."));
    } = 5;

    // The name of an option that changed; the inspector passes it on as OptionChanged.
    internal event Action<string>? Changed;

    // The culture a conversion uses now: Culture, or the current culture when it is null. Public, so a
    // view or a host shows values in the same culture the core reads typed text with (P7.21).
    public CultureInfo CultureInUse => Culture ?? CultureInfo.CurrentCulture;

    internal bool ViewToInstance => BinderControl.HasFlag(BinderControlMode.ViewToInstance);

    internal bool InstanceToView => BinderControl.HasFlag(BinderControlMode.InstanceToView);

    private void Change<T>(ref T field, T value, [CallerMemberName] string option = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        Changed?.Invoke(option);
    }

    private static double Length(double value, string name)
    {
        return double.IsFinite(value) && value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(name, value, "A length is a number from zero up.");
    }
}
