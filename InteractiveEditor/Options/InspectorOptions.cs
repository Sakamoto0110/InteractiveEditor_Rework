using System.Globalization;

namespace InteractiveEditor.Options;

// The options of one inspector, between the global ones and those of each node.
public sealed class InspectorOptions
{
    // How text typed in the view is read into numbers, dates and the rest. Null means the current
    // culture at the time of each conversion.
    public CultureInfo? Culture { get; set; }

    internal CultureInfo CultureInUse => Culture ?? CultureInfo.CurrentCulture;
}
