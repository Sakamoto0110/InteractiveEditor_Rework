namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorScrubAttribute(double multiplier) : Attribute
{
    public double Multiplier { get; } = multiplier;
}
