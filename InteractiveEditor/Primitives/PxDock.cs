namespace InteractiveEditor.Primitives;

// Where a panel docks in its container, in place of the DockStyle of WinForms in the options (P8.2),
// with the same values. An enum cannot declare conversions, so the ones with DockStyle are extension
// methods (PxDockConversions.Windows.cs).
public enum PxDock
{
    None,
    Top,
    Bottom,
    Left,
    Right,
    Fill,
}
