using System.Collections;

namespace InteractiveEditor.Options;

public sealed class FieldOptionsCollection : IEnumerable<FieldOptions>
{
    private readonly Type Target;
    private readonly IReadOnlyList<FieldOptions> Fields;
    private readonly Dictionary<string, FieldOptions> ByPath;

    internal FieldOptionsCollection(Type target, IReadOnlyList<FieldOptions> fields)
    {
        Target = target;
        Fields = fields;
        ByPath = fields.ToDictionary(f => f.Path, StringComparer.Ordinal);
    }

    public int Count => Fields.Count;

    public FieldOptions this[string path] => ByPath.TryGetValue(path, out var field)
        ? field
        : throw new KeyNotFoundException($"'{Target.Name}' has no field at path '{path}'.");

    public bool Contains(string path) => ByPath.ContainsKey(path);

    public IEnumerator<FieldOptions> GetEnumerator() => Fields.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
