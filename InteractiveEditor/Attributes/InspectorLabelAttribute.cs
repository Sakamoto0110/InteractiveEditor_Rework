namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorLabelAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}
