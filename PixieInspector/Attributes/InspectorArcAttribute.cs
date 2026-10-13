namespace PixieInspector.Attributes;

// Edits the member as the center of an arc whose width is the sibling named here (EditorKind.Arc), as
// [InspectorArc(nameof(Spread))] on a Direction: both held in radians.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorArcAttribute(string spread) : Attribute
{
    public string Spread { get; } = spread;
}
