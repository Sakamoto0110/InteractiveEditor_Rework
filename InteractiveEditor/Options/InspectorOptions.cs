using System.Globalization;

namespace InteractiveEditor.Options;

// The options of one inspector, between the global ones and those of each node.
public sealed class InspectorOptions
{
    // How text typed in the view is read into numbers, dates and the rest. Null means the current
    // culture at the time of each conversion.
    public CultureInfo? Culture { get; set; }

    // Which way values move on their own between the view and the objects.
    public BinderControlMode BinderControl { get; set; } = BinderControlMode.Automatic;

    internal CultureInfo CultureInUse => Culture ?? CultureInfo.CurrentCulture;

    internal bool ViewToInstance => BinderControl.HasFlag(BinderControlMode.ViewToInstance);

    internal bool InstanceToView => BinderControl.HasFlag(BinderControlMode.InstanceToView);
}
