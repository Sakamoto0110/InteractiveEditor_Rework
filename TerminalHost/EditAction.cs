using InteractiveEditor;
using InteractiveEditor.Options;

namespace TerminalHost;

// What the edit panel does with a row. It follows the editor the core chose for the node (P7.9), as the
// views of the other frameworks do, with what a terminal has: one edit box, whose text the core converts.
internal enum EditAction
{
    // Nothing to write: the inspector itself, a group, a header or a separator.
    None,

    // A value typed and written with SetValue: Text, Number, Toggle, Choice, Slider and Color.
    Value,

    // A value shown and never written, as a Display editor is.
    Display,

    // A button added by hand, pressed.
    Press,

    // A collection: the edit box chooses the item shown in the row below (P5.10), and a List editor
    // also adds, removes and moves items.
    Choose,
}

internal static class EditActions
{
    // A group has no editor of its own, unless it is a collection with its selector or its list.
    public static EditAction For(InspectorNode node)
    {
        return node switch
        {
            ButtonNode => EditAction.Press,
            CollectionNode { Editor: EditorKind.Selector or EditorKind.List } => EditAction.Choose,
            { IsGroup: true } or { Editor: EditorKind.Header or EditorKind.Separator } => EditAction.None,
            { Editor: EditorKind.Display } => EditAction.Display,
            _ => EditAction.Value,
        };
    }

    // Whether the row shows its objects as holding different values: only a row with a value of its own.
    public static bool ShowsMixed(InspectorNode node)
    {
        return For(node) is EditAction.Value or EditAction.Display && node.IsMixed;
    }
}
