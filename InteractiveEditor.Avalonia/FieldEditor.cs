using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using InteractiveEditor.Binding;

namespace InteractiveEditor.Avalonia;

/// <summary>
/// A control that shows and edits one value of a given type. <see cref="Show"/> pushes a value into the
/// control without raising <see cref="Edited"/>, which fires only for changes made by the user. The edit is
/// handed over as a producer so that parse errors surface where the value is applied.
/// </summary>
internal abstract class FieldEditor
{
    private bool Showing;

    public abstract Control Control { get; }

    public event Action<Func<object?>>? Edited;

    public static FieldEditor For(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(bool))
            return new BoolEditor(nullable: underlying != type);

        if (type.IsEnum)
            return new EnumEditor(type);

        if (NumericEditor.Supports(underlying))
            return new NumericEditor(type);

        if (ValueText.CanParse(type))
            return new TextEditor(type);

        return new ReadOnlyEditor();
    }

    public void Show(object? value)
    {
        Showing = true;

        try
        {
            Display(value);
        }
        finally
        {
            Showing = false;
        }
    }

    protected abstract void Display(object? value);

    protected void RaiseEdited(Func<object?> produce)
    {
        if (!Showing)
            Edited?.Invoke(produce);
    }
}

internal sealed class BoolEditor : FieldEditor
{
    private readonly CheckBox Box;

    public BoolEditor(bool nullable)
    {
        Box = new CheckBox { IsThreeState = nullable };
        Box.IsCheckedChanged += (_, _) => RaiseEdited(() => Box.IsChecked);
    }

    public override Control Control => Box;

    protected override void Display(object? value) => Box.IsChecked = (bool?)value;
}

internal sealed class EnumEditor : FieldEditor
{
    private readonly ComboBox Box;

    public EnumEditor(Type type)
    {
        Box = new ComboBox { ItemsSource = Enum.GetValues(type), HorizontalAlignment = HorizontalAlignment.Stretch };
        Box.SelectionChanged += (_, _) =>
        {
            if (Box.SelectedItem is { } value)
                RaiseEdited(() => value);
        };
    }

    public override Control Control => Box;

    protected override void Display(object? value) => Box.SelectedItem = value;
}

internal sealed class NumericEditor : FieldEditor
{
    private static readonly Dictionary<Type, (decimal Min, decimal Max)> Ranges = new()
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

    private readonly NumericUpDown Box;

    // Integral types and decimal fit NumericUpDown's decimal value exactly; float and double go to TextEditor.
    public static bool Supports(Type type) => Ranges.ContainsKey(type);

    public NumericEditor(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var nullable = underlying != type;
        var integral = underlying != typeof(decimal);
        var (min, max) = Ranges[underlying];

        Box = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Increment = 1,
            FormatString = integral ? "0" : string.Empty,
            ParsingNumberStyle = integral ? NumberStyles.Integer : NumberStyles.Number,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        Box.ValueChanged += (_, e) => RaiseEdited(() => e.NewValue switch
        {
            { } value => Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture),
            null when nullable => null,
            null => throw new InvalidOperationException($"{underlying.Name} cannot be empty.")
        });
    }

    public override Control Control => Box;

    protected override void Display(object? value) =>
        Box.Value = value == null ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
}

/// <summary>
/// Free text for anything <see cref="ValueText"/> can parse (string, float, double, char, DateTime, Guid,
/// nullable enums...). The text is committed on Enter or when focus leaves; Escape puts the shown value back.
/// </summary>
internal sealed class TextEditor : FieldEditor
{
    private readonly TextBox Box;
    private readonly Type Type;
    private string Shown = string.Empty;

    public TextEditor(Type type)
    {
        Type = type;
        Box = new TextBox();

        Box.LostFocus += (_, _) => Commit();
        Box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Box.Text = Shown;
                e.Handled = true;
            }
        };
    }

    public override Control Control => Box;

    protected override void Display(object? value)
    {
        Shown = ValueText.ToText(value);
        Box.Text = Shown;
        Box.PlaceholderText = value == null ? "null" : null;
    }

    private void Commit()
    {
        var text = Box.Text ?? string.Empty;

        if (text != Shown)
            RaiseEdited(() => ValueText.Parse(text, Type));
    }
}

internal sealed class ReadOnlyEditor : FieldEditor
{
    private readonly TextBlock Block = new() { VerticalAlignment = VerticalAlignment.Center };

    public override Control Control => Block;

    protected override void Display(object? value) => Block.Text = ValueText.Format(value);
}
