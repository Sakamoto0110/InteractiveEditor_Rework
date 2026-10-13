namespace PixieInspector.Attributes;

// The unit a number is shown and typed in (DisplayUnit): shown = held × factor + Offset, with the suffix
// after it, as [InspectorUnit("°", DisplayUnit.RadiansToDegrees)] for an angle held in radians.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorUnitAttribute(string suffix, double factor) : Attribute
{
    public string Suffix { get; } = suffix;
    public double Factor { get; } = factor;
    public double Offset { get; set; }
}
