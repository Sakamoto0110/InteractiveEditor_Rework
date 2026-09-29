namespace InteractiveEditor.Options;

// Which way values move on their own between the view and the bound objects. Every combination is
// valid: Manual is neither way, and Automatic is both. What does not move on its own waits for the
// normal flow, Apply() and Reload(); the Force* methods are for when that flow failed or does not fit.
[Flags]
public enum BinderControlMode
{
    // Nothing moves on its own.
    Manual = 0,

    // A value written through the inspector reaches the objects at once; without it, the value waits
    // in the node for Apply().
    ViewToInstance = 1,

    // A change in the objects reaches the view on its own (INotifyPropertyChanged and Refresh());
    // without it, only Reload() reads them again.
    InstanceToView = 2,

    // Both ways: the default.
    Automatic = ViewToInstance | InstanceToView,
}
