namespace InteractiveEditor.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorExpandableAttribute : Attribute
{
    public bool Collapsed { get; set; }
}
