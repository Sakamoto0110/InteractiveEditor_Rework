namespace PixieInspector.Options;

// How a number is shown and typed, against how the member holds it: shown = held × Factor + Offset, with
// the suffix after it. An angle held in radians and shown in degrees is DisplayUnit.Degrees.
public sealed record DisplayUnit
{
    // The factor of DisplayUnit.Degrees, for an attribute, which takes only constants:
    // [InspectorUnit("°", DisplayUnit.RadiansToDegrees)].
    public const double RadiansToDegrees = 180 / Math.PI;

    public DisplayUnit(string suffix, double factor, double offset = 0)
    {
        ArgumentNullException.ThrowIfNull(suffix);

        if (!double.IsFinite(factor) || factor == 0)
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "A unit's factor must be a finite number other than zero.");

        if (!double.IsFinite(offset))
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "A unit's offset must be a finite number.");

        Suffix = suffix;
        Factor = factor;
        Offset = offset;
    }

    // An angle held in radians, shown and typed in degrees.
    public static DisplayUnit Degrees { get; } = new("°", RadiansToDegrees);

    public string Suffix { get; }
    public double Factor { get; }
    public double Offset { get; }

    // Rounded to 12 significant digits, so a value that went into the member and back shows as it was
    // typed (60, not 59.99999999999999).
    public double ToShown(double held) => Significant(held * Factor + Offset);

    public double ToHeld(double shown) => (shown - Offset) / Factor;

    private static double Significant(double value)
    {
        if (value == 0 || !double.IsFinite(value))
            return value;

        var scale = Math.Pow(10, 11 - Math.Floor(Math.Log10(Math.Abs(value))));
        return double.IsFinite(scale) && double.IsFinite(value * scale) ? Math.Round(value * scale) / scale : value;
    }
}
