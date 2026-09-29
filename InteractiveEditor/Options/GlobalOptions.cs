namespace InteractiveEditor.Options;

public static class GlobalOptions
{
    // The ids of the inspectors alive. The options are read during their Create, so they stay
    // frozen until the last one is disposed.
    private static readonly HashSet<int> LiveInspectors = [];

    /// <summary>
    /// When true, a nested object only becomes a group if its member or its type has
    /// [InspectorExpandable]; otherwise it is shown as a single display field.
    /// </summary>
    public static bool RequireExpandableAttribute
    {
        get;
        set
        {
            CheckUnlocked();
            field = value;
        }
    }

    internal static void Lock(int inspectorId)
    {
        lock (LiveInspectors)
            LiveInspectors.Add(inspectorId);
    }

    internal static void Unlock(int inspectorId)
    {
        lock (LiveInspectors)
            LiveInspectors.Remove(inspectorId);
    }

    private static void CheckUnlocked()
    {
        lock (LiveInspectors)
        {
            if (LiveInspectors.Count > 0)
                throw new InvalidOperationException(
                    $"Global options are locked while inspectors are alive (ids {string.Join(", ", LiveInspectors.Order())}); dispose them first.");
        }
    }
}
