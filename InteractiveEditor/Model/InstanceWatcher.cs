using System.ComponentModel;
using InteractiveEditor.Events;

namespace InteractiveEditor.Model;

// Listens to the bound objects, to the objects of the groups and to the collections that report their
// own changes (INotifyPropertyChanged, as an ObservableCollection does), so their nodes update without
// waiting for a Refresh().
internal sealed class InstanceWatcher(RootNode root)
{
    // Every watched object, with the nodes whose members it owns: the root for a bound object, a
    // group for its object, a collection for itself. The same object can own members in more than one
    // place.
    private readonly Dictionary<INotifyPropertyChanged, List<InspectorNode>> Owners = new(ReferenceEqualityComparer.Instance);

    // Watches what the objects hold now. Called when the bind changes, and when a group's object
    // may have been replaced.
    public void Rewire()
    {
        Clear();

        foreach (var instance in root.Instances)
        {
            Watch(instance, root);

            foreach (var node in root.Where(n => n.HasMembers && n.ValueType is { IsValueType: false }))
            {
                if (node.IsCompromised)
                    continue;

                try
                {
                    Watch(node.Resolve(instance), node);
                }
                catch (Exception)
                {
                    // A getter that throws leaves nothing to watch; the read reports it.
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var watched in Owners.Keys)
            watched.PropertyChanged -= OnPropertyChanged;

        Owners.Clear();
    }

    private void Watch(object? value, InspectorNode owner)
    {
        if (value is not INotifyPropertyChanged watched)
            return;

        if (!Owners.TryGetValue(watched, out var owners))
        {
            Owners[watched] = owners = [];
            watched.PropertyChanged += OnPropertyChanged;
        }

        owners.Add(owner);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Without InstanceToView, the view reads the objects only by Reload().
        if (!root.Owner.Options.InstanceToView)
            return;

        if (sender is not INotifyPropertyChanged watched || !Owners.TryGetValue(watched, out var owners))
            return;

        var groupChanged = false;

        foreach (var owner in owners.ToList())
        {
            // A collection that reports its changes changed its items (P5.10), unless it is the
            // inspector putting one in.
            if (owner is CollectionNode collection)
            {
                if (!collection.IsCompromised && !collection.Writing)
                {
                    collection.UpdateAffected(ValueSource.Instance);
                    groupChanged = true;
                }

                continue;
            }

            // A notification with no name means that every member of the object may have changed.
            foreach (var node in owner.OwnMembers.Where(n => string.IsNullOrEmpty(e.PropertyName) || n.Name == e.PropertyName))
            {
                if (node.IsCompromised || node.Writing)
                    continue;

                if (node.HasMembers)
                {
                    node.CheckReplaced();
                    groupChanged = true;
                }

                node.UpdateAffected(ValueSource.Instance);
            }
        }

        // A group's object may be a new one now (accepted, or replaced by a closed edit), and so may
        // the item chosen in a collection.
        if (groupChanged)
            Rewire();
    }
}
