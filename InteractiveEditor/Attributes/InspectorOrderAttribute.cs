namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
