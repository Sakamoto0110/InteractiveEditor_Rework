using InteractiveEditor.Model;

namespace InteractiveEditor.Options;

public static class GlobalOptions
{
    // The ids of the inspectors alive. The options are read during their Create, so they stay
    // frozen until the last one is disposed.
    private static readonly HashSet<int> LiveInspectors = [];

    // The member names hidden in every object of a type, registered from outside (P6.7).
    private static readonly Dictionary<Type, HashSet<string>> HiddenNames = [];

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

    // Hides these members in every object of T, and of the types derived from it, in the inspectors
    // created from now on (P6.4, P6.6, P6.7). It counts with the attributes, as an [InspectorIgnore]
    // on each member, so it also serves types that cannot be annotated; the manual layer can still
    // bring a member back (Ignored = false). A name T does not have is a mistake, and throws.
    public static void Hide<T>(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        foreach (var name in names)
        {
            if (ReflectionDiscovery.FindMember(typeof(T), name) == null)
                throw new ArgumentException($"'{typeof(T).Name}' has no public field or property named '{name}'.", nameof(names));
        }

        lock (LiveInspectors)
        {
            CheckUnlocked();

            if (!HiddenNames.TryGetValue(typeof(T), out var hidden))
                HiddenNames[typeof(T)] = hidden = [];

            hidden.UnionWith(names);
        }
    }

    // Takes these names off what Hide registered for T; with no names, all of them.
    public static void Unhide<T>(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        lock (LiveInspectors)
        {
            CheckUnlocked();

            if (names.Length == 0)
                HiddenNames.Remove(typeof(T));
            else if (HiddenNames.TryGetValue(typeof(T), out var hidden))
                hidden.ExceptWith(names);
        }
    }

    // Whether Hide registered this name for the type of the object, or for a type it derives from.
    internal static bool IsHidden(Type ownerType, string name)
    {
        return HiddenNames.Any(entry => entry.Key.IsAssignableFrom(ownerType) && entry.Value.Contains(name));
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
