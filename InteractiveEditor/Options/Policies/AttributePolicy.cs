using System.Reflection;
using InteractiveEditor.Attributes;
using InteractiveEditor.Diagnostics;

namespace InteractiveEditor.Options.Policies;

internal static class AttributePolicy
{
    public static void Apply(MemberNode node, Inspector inspector)
    {
        Use<InspectorIgnoreAttribute>(node, inspector, _ => node.Ignored = true);
        Use<InspectorLabelAttribute>(node, inspector, label => node.Label = label.Text);
        Use<InspectorTooltipAttribute>(node, inspector, tooltip => node.Tooltip = tooltip.Text);
        Use<InspectorHelpAttribute>(node, inspector, help => node.Help = help.Text);
        Use<InspectorReadOnlyAttribute>(node, inspector, _ => node.ReadOnly = true);
        Use<InspectorEditorAttribute>(node, inspector, editor => node.Editor = editor.Kind);
        Use<InspectorRangeAttribute>(node, inspector, range => node.Range = new NumericRange(range.Min, range.Max, range.Step));
        Use<InspectorScrubAttribute>(node, inspector, scrub => node.ScrubMultiplier = scrub.Multiplier);
        Use<InspectorOrderAttribute>(node, inspector, order => node.Order = order.Order);

        // [InspectorExpandable] counts on the member or on its type, and only when there is something to open.
        Use(node, inspector,
            () => node.Member.GetCustomAttribute<InspectorExpandableAttribute>()
                ?? (Nullable.GetUnderlyingType(node.ValueType) ?? node.ValueType).GetCustomAttribute<InspectorExpandableAttribute>(),
            expandable =>
            {
                if (!node.HasMembers)
                    return;

                node.Expandable = true;
                node.Collapsed = expandable.Collapsed;
            });
    }

    private static void Use<TAttribute>(MemberNode node, Inspector inspector, Action<TAttribute> apply)
        where TAttribute : Attribute
    {
        Use(node, inspector, () => node.Member.GetCustomAttribute<TAttribute>(), apply);
    }

    // An attribute that cannot be read or applied is skipped: the node keeps what the reflection
    // decided, and a subscriber can set the option itself.
    private static void Use<TAttribute>(MemberNode node, Inspector inspector, Func<TAttribute?> read, Action<TAttribute> apply)
        where TAttribute : Attribute
    {
        try
        {
            if (read() is { } attribute)
                apply(attribute);
        }
        catch (Exception e)
        {
            var name = typeof(TAttribute).Name[..^"Attribute".Length];

            inspector.OnDiscoveryFailed(node.Path, FailureSeverity.WorkedAround, e,
                $"[{name}] on '{node.Path}' was ignored.",
                $"Fix the attribute, or set the option on inspector[\"{node.Path}\"] after the Create.");
        }
    }
}
