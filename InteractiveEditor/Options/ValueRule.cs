using InteractiveEditor.Model;

namespace InteractiveEditor.Options;

// A rule for the value, applied in the node's order after the text is converted.
public sealed class ValueRule(Func<object?, object?> apply)
{
    public object? Apply(object? value) => apply(value);

    // Raises a number below the minimum to it; other values pass as they are.
    public static ValueRule Min(double min) => new(value => ValueConverter.Clamp(value, min, double.MaxValue));

    // Lowers a number above the maximum to it; other values pass as they are.
    public static ValueRule Max(double max) => new(value => ValueConverter.Clamp(value, double.MinValue, max));
}
