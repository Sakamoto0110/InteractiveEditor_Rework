using System.Reflection;
using InteractiveEditor.Attributes;

namespace InteractiveEditor.Options.Policies;

internal sealed class AttributePolicy : IFieldPolicy
{
    public void Apply(InspectorNode node)
    {
        if (node.Descriptor is not { } member)
            return;

        var info = member.MemberInfo;

        if (info.GetCustomAttribute<InspectorIgnoreAttribute>() != null)
            node.Ignored = true;

        if (info.GetCustomAttribute<InspectorLabelAttribute>() is { } label)
            node.Label = label.Text;

        if (info.GetCustomAttribute<InspectorTooltipAttribute>() is { } tooltip)
            node.Tooltip = tooltip.Text;

        if (info.GetCustomAttribute<InspectorHelpAttribute>() is { } help)
            node.Help = help.Text;

        if (info.GetCustomAttribute<InspectorReadOnlyAttribute>() != null)
            node.ReadOnly = true;

        if (info.GetCustomAttribute<InspectorEditorAttribute>() is { } editor)
            node.Editor = editor.Kind;

        if (info.GetCustomAttribute<InspectorRangeAttribute>() is { } range)
            node.Range = new NumericRange(range.Min, range.Max, range.Step);

        if (info.GetCustomAttribute<InspectorScrubAttribute>() is { } scrub)
            node.ScrubMultiplier = scrub.Multiplier;

        if (info.GetCustomAttribute<InspectorOrderAttribute>() is { } order)
            node.Order = order.Order;

        var fieldType = Nullable.GetUnderlyingType(member.FieldType) ?? member.FieldType;
        var expandable = info.GetCustomAttribute<InspectorExpandableAttribute>()
            ?? fieldType.GetCustomAttribute<InspectorExpandableAttribute>();

        if (expandable != null && node is Inspector)
        {
            node.Expandable = true;
            node.Collapsed = expandable.Collapsed;
        }
    }
}
