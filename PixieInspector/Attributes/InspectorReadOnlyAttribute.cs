namespace PixieInspector.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorReadOnlyAttribute : Attribute
{
}
