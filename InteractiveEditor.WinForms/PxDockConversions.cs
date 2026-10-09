using System.Windows.Forms;
using InteractiveEditor.Primitives;

namespace InteractiveEditor.WinForms;

// The conversions between PxDock and the DockStyle of WinForms, which has the same values. An enum
// cannot declare conversion operators, and an extension block cannot either (CS9282), so they are
// extension methods. A value outside the enum throws.
public static class PxDockConversions
{
    public static DockStyle ToDockStyle(this PxDock dock) => dock switch
    {
        PxDock.None => DockStyle.None,
        PxDock.Top => DockStyle.Top,
        PxDock.Bottom => DockStyle.Bottom,
        PxDock.Left => DockStyle.Left,
        PxDock.Right => DockStyle.Right,
        PxDock.Fill => DockStyle.Fill,
        _ => throw new ArgumentOutOfRangeException(nameof(dock), dock, "Not a PxDock value."),
    };

    public static PxDock ToPxDock(this DockStyle dock) => dock switch
    {
        DockStyle.None => PxDock.None,
        DockStyle.Top => PxDock.Top,
        DockStyle.Bottom => PxDock.Bottom,
        DockStyle.Left => PxDock.Left,
        DockStyle.Right => PxDock.Right,
        DockStyle.Fill => PxDock.Fill,
        _ => throw new ArgumentOutOfRangeException(nameof(dock), dock, "Not a DockStyle value."),
    };
}
