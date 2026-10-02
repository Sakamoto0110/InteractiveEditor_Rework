using InteractiveEditor.Options;

namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorScrubAttribute(double multiplier) : Attribute
{
    public double Multiplier { get; } = multiplier;

    // Across by default; up and down by choice (P7.18).
    public ScrubAxis Axis { get; set; }
}
