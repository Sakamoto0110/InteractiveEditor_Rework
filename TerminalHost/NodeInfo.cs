using System.Globalization;
using System.Reflection;
using InteractiveEditor;
using InteractiveEditor.Options;

namespace TerminalHost;

// What the host says about the inspector and its nodes: the line of each one in the tree and in the
// dump, the hint of the edit panel, and the description of the Info panel. Values are what the view
// holds (ViewValue), as a view shows them; only the binding check reads the objects.
internal static class NodeInfo
{
    public static string Title(Inspector inspector)
    {
        var count = inspector.Instances.Count;
        return count == 1 ? inspector.Name : $"{inspector.Name} ({count} objects)";
    }

    // The row's label, with its value or what stands for it; a failure of the node, or a branch disabled
    // by a replaced group, shows after it.
    public static string Label(InspectorNode node)
    {
        var line = EditActions.For(node) switch
        {
            EditAction.Press => $"{node.Label} [{((ButtonNode)node).Text}]",
            EditAction.Choose => $"{node.Label} ({Items((CollectionNode)node)})",
            EditAction.None when node.Editor == EditorKind.Separator => "----",

            // A group whose object is null has nothing to open.
            EditAction.None when node is MemberNode { IsGroup: true, ViewValue: null } && node.Inspector.Instances.Count > 0 => $"{node.Label} = null",
            EditAction.None => node.Label,
            _ => $"{node.Label} = {ValueText(node)}",
        };

        if (node.IsCompromised)
            return $"{line}  <disabled>";

        return node.Failure is { } failure ? $"{line}  <{failure.Reason}>" : line;
    }

    // The editor and the state of a node, for the dump.
    public static string Tags(InspectorNode node)
    {
        return string.Join(", ", States(node).Prepend(EditorName(node)));
    }

    // A group opens instead of having an editor; a collection is a group with its selector or its list.
    private static string EditorName(InspectorNode node)
    {
        return node.IsGroup && node is not CollectionNode ? "group" : node.Editor.ToString();
    }

    // What the view makes of the node's options and of its objects now.
    private static IEnumerable<string> States(InspectorNode node)
    {
        if (node.ReadOnly)
            yield return "read-only";

        if (node.IsGroup && node.Collapsed)
            yield return "collapsed";

        if (node.Ignored)
            yield return "ignored";

        if (!node.Visible)
            yield return "hidden";

        if (EditActions.ShowsMixed(node))
            yield return "mixed";

        if (node.HasPendingValue)
            yield return "pending";

        if (node.IsCompromised)
            yield return "disabled";
    }

    // One line under the edit box: what Set does with the row, and what it takes.
    public static string Hint(InspectorNode? node)
    {
        if (node == null)
            return "Select a row to edit it.";

        var action = EditActions.For(node);

        if (node.IsCompromised)
            return "Disabled: the object of a group above was replaced outside the inspector (F7 starts over).";

        if (action == EditAction.Choose)
        {
            var items = ((CollectionNode)node).Items.Select((item, index) => $"[{index}] {NodeText.Format(node, item)}");
            return $"Index of the item shown (-1 for none): {string.Join(", ", items)}";
        }

        if (action is EditAction.None or EditAction.Display || node.ReadOnly)
        {
            return action switch
            {
                EditAction.None when node.IsGroup => "A group: edit its fields.",
                EditAction.None => "Nothing to edit here.",
                EditAction.Display => "Shows a value; there is nothing to write.",
                _ => "Read-only.",
            };
        }

        if (action == EditAction.Press)
            return "Press runs the button's action.";

        var hint = node.Editor switch
        {
            EditorKind.Choice => $"Choices: {string.Join(", ", node.GetChoices().Select(choice => NodeText.Format(node, choice)))}",
            EditorKind.Toggle => "True or False.",
            EditorKind.Color => "A color: (r, g, b, a) or #RRGGBB[AA].",
            EditorKind.Slider when node.Range is { } range => string.Create(NodeText.CultureOf(node.Inspector),
                $"From {range.Min} to {range.Max}, step {range.Step}; the core clamps the value."),
            _ when node.ValueType is { } type && Nullable.GetUnderlyingType(type) != null => "Empty or null for no value.",
            _ => "Enter sets the text; the core converts it.",
        };

        return EditActions.ShowsMixed(node) ? $"The objects hold different values; a value goes to all of them. {hint}" : hint;
    }

    public static string Describe(Inspector inspector)
    {
        var instances = inspector.Instances;
        var failures = inspector.Report.Failures;

        var lines = new List<(string Key, string Value)>
        {
            ("Kind", "Inspector"),
            ("Name", inspector.Name),
            ("Id", inspector.Id.ToString(CultureInfo.InvariantCulture)),
            ("Mode", inspector.Mode.ToString()),
            ("Objects", instances.Count == 0 ? "none" : string.Join(", ", instances.Select(i => NodeText.TypeName(i.GetType())))),
            ("Nodes", $"{inspector.Count()}, {inspector.Rows.Count()} shown as rows"),
            ("Binder", inspector.Options.BinderControl.ToString()),
            ("Culture", NodeText.CultureOf(inspector) is { Name: "" } ? "invariant" : NodeText.CultureOf(inspector).Name),
            ("Pending", inspector.HasPendingValues ? "yes" : "no"),
            ("Report", failures.Count == 0 ? "no failures" : $"{failures.Count} failures (see the log)"),
        };

        return Join(lines);
    }

    public static string Describe(InspectorNode node)
    {
        var member = (node as MemberNode)?.Member;

        var lines = new List<(string Key, string Value)>
        {
            ("Kind", member == null ? node.GetType().Name : $"{node.GetType().Name}, {member.MemberType.ToString().ToLowerInvariant()}"),
            ("Name", node.Name),
        };

        if (node.Label != node.Name)
            lines.Add(("Label", node.Label));

        lines.Add(("Path", node.Path));

        if (node.ValueType is { } type)
            lines.Add(("Type", Owner(node, member) is { } owner ? $"{NodeText.TypeName(type)}, in {NodeText.TypeName(owner)}" : NodeText.TypeName(type)));

        lines.Add(("Access", Access(node, member)));
        lines.Add(("Editor", Editor(node)));
        lines.Add(("State", string.Join(", ", States(node).DefaultIfEmpty("-"))));
        lines.Add(("Parent", node.Parent is { Parent: not null } parent ? parent.Path : "-"));

        if (node.HasMembers)
            lines.Add(("Children", node.Count(child => child.Parent == node).ToString(CultureInfo.InvariantCulture)));

        if (node is CollectionNode collection)
            lines.Add(("Items", Items(collection)));

        if (EditActions.For(node) is EditAction.Value or EditAction.Display)
        {
            var value = node.ViewValue;
            lines.Add(("Value", ValueText(node)));
            lines.Add(("Value type", value == null ? "-" : NodeText.TypeName(value.GetType())));
        }

        if (node.Failure is { } failure)
            lines.Add(("Failure", $"{failure.Message} {failure.Reason}"));

        if (node.Tooltip is { } tooltip)
            lines.Add(("Tooltip", tooltip));

        if (node.Help is { } help)
            lines.Add(("Help", help));

        return Join(lines);
    }

    // What the view holds for the row; objects that hold different values show none (P7.19).
    private static string ValueText(InspectorNode node)
    {
        return EditActions.ShowsMixed(node) ? "<mixed>" : NodeText.Format(node, node.ViewValue);
    }

    private static string Items(CollectionNode collection)
    {
        var count = collection.Items.Count;
        var chosen = collection.SelectedIndex < 0 ? "none chosen" : $"#{collection.SelectedIndex}";

        return count == 0 ? "no items" : $"{count} items, {chosen}";
    }

    // The editor option, with what goes along with it.
    private static string Editor(InspectorNode node)
    {
        var culture = NodeText.CultureOf(node.Inspector);
        var parts = new List<string> { EditorName(node) };

        if (node.Range is { } range)
            parts.Add(string.Create(culture, $"range {range.Min}..{range.Max} step {range.Step}"));

        if (node.ScrubMultiplier is { } scrub)
            parts.Add(string.Create(culture, $"scrub x{scrub} {node.ScrubAxis.ToString().ToLowerInvariant()}"));

        if (node is ButtonNode button)
            parts.Add($"\"{button.Text}\"");

        return string.Join(", ", parts);
    }

    // The type the member belongs to; the collection, for the item chosen in it.
    private static Type? Owner(InspectorNode node, MemberInfo? member)
    {
        return member?.DeclaringType ?? (node as ItemNode)?.Collection.ValueType;
    }

    private static string Access(InspectorNode node, MemberInfo? member)
    {
        return member switch
        {
            FieldInfo fi => fi.IsInitOnly || fi.IsLiteral ? "get" : "get, set",
            PropertyInfo pi => string.Join(", ", new[]
            {
                pi.GetMethod is { IsPublic: true } ? "get" : null,
                pi.SetMethod is { IsPublic: true } ? "set" : pi.SetMethod != null ? "non-public set" : null,
            }.OfType<string>()),
            _ => node switch
            {
                ItemNode => "the item chosen in the collection",
                ButtonNode => "an action added by hand",
                DisplayNode => "a getter added by hand",
                _ => "-",
            },
        };
    }

    private static string Join(IEnumerable<(string Key, string Value)> lines)
    {
        return string.Join(Environment.NewLine, lines.Select(l => $"{l.Key,-11} {l.Value}"));
    }
}
