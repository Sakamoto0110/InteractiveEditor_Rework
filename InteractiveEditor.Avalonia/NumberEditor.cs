using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;

namespace InteractiveEditor.Avalonia;

// A spinner for a Number (P7.9) of a type that fits its decimal value exactly: the integers and decimal;
// float and double go to a text box. It stops at the node's range, or at the limits of the type, steps by
// the range's step or by one, and reads what is typed in the inspector's culture. It writes on every
// change of its value (P2.12). Empty, it writes null, which a nullable number takes and any other refuses
// on the row; the placeholder tells null from the grey dash of objects with different values (P7.19).
internal sealed class NumberEditor : AvaloniaEditor
{
    private static readonly Dictionary<Type, (decimal Min, decimal Max)> Limits = new()
    {
        [typeof(byte)] = (byte.MinValue, byte.MaxValue),
        [typeof(sbyte)] = (sbyte.MinValue, sbyte.MaxValue),
        [typeof(short)] = (short.MinValue, short.MaxValue),
        [typeof(ushort)] = (ushort.MinValue, ushort.MaxValue),
        [typeof(int)] = (int.MinValue, int.MaxValue),
        [typeof(uint)] = (uint.MinValue, uint.MaxValue),
        [typeof(long)] = (long.MinValue, long.MaxValue),
        [typeof(ulong)] = (ulong.MinValue, ulong.MaxValue),
        [typeof(decimal)] = (decimal.MinValue, decimal.MaxValue),
    };

    private readonly NumericUpDown Box = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly (decimal Min, decimal Max) Limit;

    // Set while the view puts a value in the spinner, so only the user's change writes.
    private bool Showing;

    public NumberEditor(AvaloniaRow row) : base(row)
    {
        var type = Underlying(Node.ValueType!);
        var integral = type != typeof(decimal);

        Limit = Limits[type];
        Box.FormatString = integral ? "0" : string.Empty;
        Box.ParsingNumberStyle = integral ? NumberStyles.Integer : NumberStyles.Number;
        Box.ValueChanged += OnValueChanged;
    }

    public override Control Control => Box;

    public static bool Fits(Type? type) => type != null && Limits.ContainsKey(Underlying(type));

    public override void ShowValue()
    {
        var mixed = Row.Mixed;
        var value = Node.ViewValue;
        var (min, max) = Limit;
        var step = 1m;

        if (Node.Range is { } range)
        {
            min = Within(range.Min, Limit);
            max = Math.Max(min, Within(range.Max, Limit));
            step = range.Step > 0 ? Within(range.Step, Limit) : step;
        }

        Showing = true;

        try
        {
            Box.NumberFormat = Node.Inspector.Options.CultureInUse.NumberFormat;
            Box.Minimum = min;
            Box.Maximum = max;
            Box.Increment = step;
            Box.PlaceholderText = mixed ? "—" : value == null ? "null" : null;
            Box.Value = mixed || value == null ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        finally
        {
            Showing = false;
        }
    }

    private static Type Underlying(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    // A double of the range as a decimal, kept within the limits of the type.
    private static decimal Within(double value, (decimal Min, decimal Max) limit)
    {
        if (value <= (double)limit.Min)
            return limit.Min;

        if (value >= (double)limit.Max)
            return limit.Max;

        return (decimal)value;
    }

    // The decimal goes as it is: the core converts a number into the member's type.
    private void OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (Showing)
            return;

        var value = e.NewValue;
        Row.Write(() => Node.SetValue(value));
    }
}
