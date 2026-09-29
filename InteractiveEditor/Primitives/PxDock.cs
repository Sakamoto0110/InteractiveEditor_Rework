namespace InteractiveEditor.Primitives;

// Where a panel docks in its container, in place of the DockStyle of WinForms in the options (P8.2),
// with the same values. The conversions come with the views.
public enum PxDock
{
    None,
    Top,
    Bottom,
    Left,
    Right,
    Fill,
}
