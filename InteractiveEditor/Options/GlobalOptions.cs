namespace InteractiveEditor.Options;

public static class GlobalOptions
{
    /// <summary>
    /// When true, a nested object only becomes a group if its member or its type has
    /// [InspectorExpandable]; otherwise it is shown as a single display field.
    /// </summary>
    public static bool RequireExpandableAttribute { get; set; }
}
