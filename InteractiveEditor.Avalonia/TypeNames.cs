namespace InteractiveEditor.Avalonia;

// The name of a type as C# writes it, for the headers of the groups and the tooltips of the labels:
// Int32? and List<Moo> instead of Nullable`1 and List`1.
internal static class TypeNames
{
    public static string Of(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{Of(underlying)}?";

        if (type.IsArray)
            return $"{Of(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";

        var tick = type.Name.IndexOf('`');

        if (!type.IsGenericType || tick < 0)
            return type.Name;

        return $"{type.Name[..tick]}<{string.Join(", ", type.GetGenericArguments().Select(Of))}>";
    }
}
