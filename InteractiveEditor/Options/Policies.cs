using System.Reflection;
using System.Runtime.CompilerServices;
using InteractiveEditor.Attributes;
using InteractiveEditor.Model;

namespace InteractiveEditor.Options;

internal static class Policies
{
    private static readonly HashSet<Type> NumberTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
    ];

    // The layers, in order: reflection, then attributes. Whatever the caller sets afterwards comes last.
    public static void Apply(InspectorNode node)
    {
        if (node.Member is not { } member)
            return;

        FromReflection(node, member);
        FromAttributes(node, member);
    }

    private static void FromReflection(InspectorNode node, MemberInfo member)
    {
        node.Label = member.Name;
        node.Editor = EditorFor(node.ValueType!);
        node.ReadOnly = !IsPubliclyWritable(member);
        node.Expandable = node.HasMembers && !GlobalOptions.RequireExpandableAttribute;
    }

    private static void FromAttributes(InspectorNode node, MemberInfo member)
    {
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

    private static EditorKind EditorFor(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return EditorKind.Choice;

        if (type == typeof(bool))
            return EditorKind.Toggle;

        if (NumberTypes.Contains(type))
            return EditorKind.Number;

        if (type == typeof(object) || !ReflectionDiscovery.IsTerminal(type))
            return EditorKind.Display;

        return EditorKind.Text;
    }

    private static bool IsPubliclyWritable(MemberInfo member)
    {
        return member switch
        {
            FieldInfo fi => !fi.IsInitOnly && !fi.IsLiteral,
            PropertyInfo pi => pi.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter),
            _ => false
        };
    }

    // init accessors are public setters marked with the IsExternalInit modifier
    private static bool IsInitOnly(MethodInfo setter)
    {
        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
    }
}
