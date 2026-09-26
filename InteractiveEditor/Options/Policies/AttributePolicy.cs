using System.Reflection;
using InteractiveEditor.Attributes;

namespace InteractiveEditor.Options.Policies;

internal sealed class AttributePolicy : IFieldPolicy
{
    public void Apply(FieldOptions field)
    {
        if (field.Member is not { } member)
            return;

        var info = member.MemberInfo;

        if (info.GetCustomAttribute<InspectorIgnoreAttribute>() != null)
            field.Ignored = true;

        if (info.GetCustomAttribute<InspectorLabelAttribute>() is { } label)
            field.Label = label.Text;

        if (info.GetCustomAttribute<InspectorTooltipAttribute>() is { } tooltip)
            field.Tooltip = tooltip.Text;

        if (info.GetCustomAttribute<InspectorHelpAttribute>() is { } help)
            field.Help = help.Text;

        if (info.GetCustomAttribute<InspectorReadOnlyAttribute>() != null)
            field.ReadOnly = true;

        if (info.GetCustomAttribute<InspectorEditorAttribute>() is { } editor)
            field.Editor = editor.Kind;

        if (info.GetCustomAttribute<InspectorRangeAttribute>() is { } range)
            field.Range = new NumericRange(range.Min, range.Max, range.Step);

        if (info.GetCustomAttribute<InspectorScrubAttribute>() is { } scrub)
            field.ScrubMultiplier = scrub.Multiplier;

        if (info.GetCustomAttribute<InspectorOrderAttribute>() is { } order)
            field.Order = order.Order;

        var fieldType = Nullable.GetUnderlyingType(member.FieldType) ?? member.FieldType;
        var expandable = info.GetCustomAttribute<InspectorExpandableAttribute>()
            ?? fieldType.GetCustomAttribute<InspectorExpandableAttribute>();

        if (expandable != null && field.HasMembers)
        {
            field.Expandable = true;
            field.Collapsed = expandable.Collapsed;
        }
    }
}
