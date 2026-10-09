using InteractiveEditor.Options;

namespace InteractiveEditor.ImGui;

// The widgets of one editor (P7.9), drawn in the second column of its row on every frame and written
// through the row. ImGui keeps no widget state between frames, so the value is read from the node each
// time; an editor keeps only what has to last across frames (the text being typed, the list of choices
// while it is open).
internal abstract class ImGuiEditor
{
    // What a row shows for objects that hold different values (P7.19). The other views use an em dash,
    // which ImGui's default font (Latin-1) does not have.
    protected const string MixedText = "--";

    protected ImGuiEditor(ImGuiRow row)
    {
        Row = row;
    }

    protected ImGuiRow Row { get; }

    protected InspectorNode Node => Row.Node;

    // Whether the editor takes input: not on a read-only node, nor in a branch whose group was replaced
    // outside the inspector (P3.4).
    public virtual bool Enabled => !Node.ReadOnly && !Node.IsCompromised;

    // The editor of each kind (P7.9); a group, a header and a separator have none, and a selector or a
    // list on something that is not a collection falls back to text.
    public static ImGuiEditor? For(ImGuiRow row, EditorKind? kind) => kind switch
    {
        null or EditorKind.Header or EditorKind.Separator => null,
        EditorKind.Number => new NumberEditor(row),
        EditorKind.Toggle => new ToggleEditor(row),
        EditorKind.Choice => new ChoiceEditor(row),
        EditorKind.Slider => new SliderEditor(row),
        EditorKind.Color => new ColorEditor(row),
        EditorKind.Button => new ButtonEditor(row),
        EditorKind.Display => new DisplayEditor(row),
        EditorKind.Selector when row.Node is CollectionNode collection => new SelectorEditor(row, collection),
        EditorKind.List when row.Node is CollectionNode collection => new ListEditor(row, collection),
        _ => new TextEditor(row),
    };

    // Draws the widgets; the first one takes the width the row set for it, the whole column.
    public abstract void Draw();

    // A value as text, in the inspector's culture, which is also the one that reads what is typed (P2.7).
    // A ToString that throws is the row's fault, and the name of the type shows in its place, so a widget
    // that lists values can be closed in the same frame.
    protected string Format(object? value)
    {
        try
        {
            return value is IFormattable formattable
                ? formattable.ToString(null, Node.Inspector.Options.CultureInUse)
                : value?.ToString() ?? string.Empty;
        }
        catch (Exception e)
        {
            Row.OnFault(e);
            return value!.GetType().Name;
        }
    }

    // An item of a collection, as the selector and the list show it.
    protected string ItemText(int index, object? item)
    {
        return $"[{index}] {Format(item)}";
    }
}
