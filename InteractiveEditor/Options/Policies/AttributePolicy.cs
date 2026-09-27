using System.Reflection;
using InteractiveEditor.Attributes;

namespace InteractiveEditor.Options.Policies;

internal static class AttributePolicy
{
    public static void Apply(InspectorNode node)
    {
        if (node.Member is not { } member)
            return;

        if (member.GetCustomAttribute<InspectorIgnoreAttribute>() != null)
            node.Ignored = true;

        if (member.GetCustomAttribute<InspectorLabelAttribute>() is { } label)
            node.Label = label.Text;

        if (member.GetCustomAttribute<InspectorTooltipAttribute>() is { } tooltip)
            node.Tooltip = tooltip.Text;

        if (member.GetCustomAttribute<InspectorHelpAttribute>() is { } help)
            node.Help = help.Text;

        if (member.GetCustomAttribute<InspectorReadOnlyAttribute>() != null)
            node.ReadOnly = true;

        if (member.GetCustomAttribute<InspectorEditorAttribute>() is { } editor)
            node.Editor = editor.Kind;

        if (member.GetCustomAttribute<InspectorRangeAttribute>() is { } range)
            node.Range = new NumericRange(range.Min, range.Max, range.Step);

        if (member.GetCustomAttribute<InspectorScrubAttribute>() is { } scrub)
            node.ScrubMultiplier = scrub.Multiplier;

        if (member.GetCustomAttribute<InspectorOrderAttribute>() is { } order)
            node.Order = order.Order;

        var fieldType = Nullable.GetUnderlyingType(node.ValueType!) ?? node.ValueType!;
        var expandable = member.GetCustomAttribute<InspectorExpandableAttribute>()
            ?? fieldType.GetCustomAttribute<InspectorExpandableAttribute>();

        if (expandable != null && node.HasMembers)
        {
            node.Expandable = true;
            node.Collapsed = expandable.Collapsed;
        }
    }
}
