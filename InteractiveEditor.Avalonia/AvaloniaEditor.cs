using Avalonia.Controls;
using InteractiveEditor.Options;

namespace InteractiveEditor.Avalonia;

// The control of one editor (P7.9), shown and written through its row.
internal abstract class AvaloniaEditor
{
    protected AvaloniaEditor(AvaloniaRow row)
    {
        Row = row;
    }

    public abstract Control Control { get; }

    // Whether the control shows the error of a failure under itself, as Avalonia's data validation does for
    // a text box, a combo box, a spinner, a slider and a list box; under any other, the row writes it.
    public virtual bool ShowsErrors => true;

    // The control that shows the error, the one inside for an editor made of several.
    public virtual Control ErrorTarget => Control;

    protected AvaloniaRow Row { get; }

    protected InspectorNode Node => Row.Node;

    // The editor of each kind (P7.9); a header has none. A number of a type that fits the spinner's decimal
    // exactly (an integer or a decimal) gets the spinner, any other number a text box, and a selector or a
    // list on something that is not a collection falls back to text.
    public static AvaloniaEditor? For(AvaloniaRow row, EditorKind? kind) => kind switch
    {
        null or EditorKind.Header => null,
        EditorKind.Number when NumberEditor.Fits(row.Node.ValueType) => new NumberEditor(row),
        EditorKind.Toggle => new ToggleEditor(row),
        EditorKind.Choice => new ChoiceEditor(row),
        EditorKind.Slider => new SliderEditor(row),
        EditorKind.Color => new ColorEditor(row),
        EditorKind.Button => new ButtonEditor(row),
        EditorKind.Display => new DisplayEditor(row),
        EditorKind.Separator => new SeparatorEditor(row),
        EditorKind.Selector when row.Node is CollectionNode collection => new SelectorEditor(row, collection),
        EditorKind.List when row.Node is CollectionNode collection => new ListEditor(row, collection),
        _ => new TextEditor(row),
    };

    public abstract void ShowValue();

    public virtual void ShowState(bool readOnly, bool enabled)
    {
        Control.IsEnabled = enabled && !readOnly;
    }

    // A value as text, in the inspector's culture, which is also the one that reads what is typed (P2.7).
    protected string Format(object? value)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, Node.Inspector.Options.CultureInUse)
            : value?.ToString() ?? string.Empty;
    }

    // An item of a collection, as the selector and the list show it.
    protected string ItemText(int index, object? item)
    {
        return $"[{index}] {Format(item)}";
    }
}
