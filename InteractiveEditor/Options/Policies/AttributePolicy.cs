using System.Reflection;
using InteractiveEditor.Attributes;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Model;

namespace InteractiveEditor.Options.Policies;

internal static class AttributePolicy
{
    public static void Apply(MemberNode node, Inspector inspector)
    {
        // Whether an attribute chose how the member is edited: a type with more than one editor needs it.
        // The item of a collection has no member, so only its type counts (P5.10).
        var chosen = node.Member != null && ApplyMember(node, inspector);

        // [InspectorExpandable] counts on the member or on its type, and only when there is something to open.
        Use(node, inspector,
            () => node.Member?.GetCustomAttribute<InspectorExpandableAttribute>()
                ?? (Nullable.GetUnderlyingType(node.ValueType) ?? node.ValueType).GetCustomAttribute<InspectorExpandableAttribute>(),
            expandable =>
            {
                if (!node.HasMembers)
                    return;

                node.Expandable = true;
                node.Collapsed = expandable.Collapsed;
                chosen = true;
            });

        // Without a choice, a type with more than one editor stays a Display row, and the Create says
        // so (P5.8); a subscriber can choose right there and mark it handled.
        if (!chosen && ReflectionDiscovery.HasManyEditors(node.ValueType))
        {
            var type = Nullable.GetUnderlyingType(node.ValueType) ?? node.ValueType;

            inspector.OnDiscoveryFailed(node.Path, FailureSeverity.WorkedAround,
                new NotSupportedException($"{type.Name} fits more than one editor, and none was chosen."),
                $"'{node.Path}' shows as Display until an editor is chosen.",
                node.Member == null
                    ? "Set Editor or Expandable on the node."
                    : "Choose one with [InspectorEditor] or [InspectorExpandable], or set Editor or Expandable on the node.");
        }
    }

    // The attributes on the member itself; true when [InspectorEditor] chose the editor.
    private static bool ApplyMember(MemberNode node, Inspector inspector)
    {
        var chosen = false;

        // [InspectorReadOnly] brings back a member the reflection hid for its setter, so it goes
        // before [InspectorIgnore], which has the last word.
        Use<InspectorReadOnlyAttribute>(node, inspector, _ =>
        {
            node.ReadOnly = true;
            node.Ignored = false;
        });
        Use<InspectorIgnoreAttribute>(node, inspector, _ => node.Ignored = true);

        // A name hidden from outside for the object's type counts as an [InspectorIgnore] (P6.7).
        if (node.Member!.ReflectedType is { } owner && GlobalOptions.IsHidden(owner, node.Name))
            node.Ignored = true;

        Use<InspectorLabelAttribute>(node, inspector, label => node.Label = label.Text);
        Use<InspectorTooltipAttribute>(node, inspector, tooltip => node.Tooltip = tooltip.Text);
        Use<InspectorHelpAttribute>(node, inspector, help => node.Help = help.Text);
        Use<InspectorEditorAttribute>(node, inspector, editor =>
        {
            node.Editor = editor.Kind;
            chosen = true;
        });
        // A collection has no value of its own to limit or scrub: its range and scrubbing go to the
        // row of its item, which is where the numbers are (P5.11).
        var valued = node is CollectionNode collection ? collection.Item : node;
        Use<InspectorRangeAttribute>(node, inspector, range => valued.Range = new NumericRange(range.Min, range.Max, range.Step));
        Use<InspectorScrubAttribute>(node, inspector, scrub => valued.ScrubMultiplier = scrub.Multiplier);
        Use<InspectorOrderAttribute>(node, inspector, order => node.Order = order.Order);

        return chosen;
    }

    private static void Use<TAttribute>(MemberNode node, Inspector inspector, Action<TAttribute> apply)
        where TAttribute : Attribute
    {
        Use(node, inspector, () => node.Member!.GetCustomAttribute<TAttribute>(), apply);
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
