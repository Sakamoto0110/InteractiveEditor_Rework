using InteractiveEditor.Model;
using InteractiveEditor.Options.Policies;

namespace InteractiveEditor.Options;

internal static class OptionsResolver
{
    private static readonly IFieldPolicy[] Policies = [new ReflectionPolicy(), new AttributePolicy()];

    public static IReadOnlyList<FieldOptions> Resolve(Type target, Action<FieldOptionsCollection>? configure)
    {
        var descriptors = ReflectionDiscovery.ResolveFor(target);
        var parents = descriptors.Select(d => ParentPath(d.FullPath)).ToHashSet(StringComparer.Ordinal);

        var fields = descriptors
            .Select(d => new FieldOptions(d, hasMembers: parents.Contains(d.FullPath)))
            .ToList();

        foreach (var field in fields)
        {
            foreach (var policy in Policies)
                policy.Apply(field);
        }

        configure?.Invoke(new FieldOptionsCollection(target, fields));

        var children = fields.ToLookup(f => ParentPath(f.Path), StringComparer.Ordinal);
        var resolved = new List<FieldOptions>(fields.Count);

        Collect(string.Empty, children, resolved);

        return resolved;
    }

    private static void Collect(string parentPath, ILookup<string, FieldOptions> children, List<FieldOptions> resolved)
    {
        foreach (var field in children[parentPath].OrderBy(f => f.Order))
        {
            if (field.Ignored)
                continue;

            resolved.Add(field);

            if (field.IsGroup)
                Collect(field.Path, children, resolved);
        }
    }

    private static string ParentPath(string path)
    {
        var separator = path.LastIndexOf('.');

        return separator == -1 ? string.Empty : path[..separator];
    }
}
