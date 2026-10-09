using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InteractiveEditor.Options;

namespace InteractiveEditor.Wpf;

// The control of one editor (P7.9), shown and written through its row.
internal abstract class WpfEditor
{
    // The background of an editor whose node failed (P7.11).
    protected static readonly SolidColorBrush FailedBrush = WpfInspectorView.Frozen(Color.FromRgb(255, 224, 224));

    protected WpfEditor(WpfRow row)
    {
        Row = row;
    }

    public abstract FrameworkElement Control { get; }

    // What the editor puts in the row's canvas: its control, and anything drawn over it.
    public virtual IEnumerable<UIElement> Elements => [Control];

    protected WpfRow Row { get; }

    protected InspectorNode Node => Row.Node;

    // The editor of each kind (P7.9); a header has none, and a selector or a list on something that is
    // not a collection falls back to text.
    public static WpfEditor? For(WpfRow row, EditorKind? kind) => kind switch
    {
        null or EditorKind.Header => null,
        EditorKind.Toggle => new ToggleEditor(row),
        EditorKind.Choice => new ChoiceEditor(row),
        EditorKind.Slider => new SliderEditor(row),
        EditorKind.Color => new ColorEditor(row),
        EditorKind.Button => new ButtonEditor(row),
        EditorKind.Display => new TextEditor(row, display: true),
        EditorKind.Separator => new SeparatorEditor(row),
        EditorKind.Selector when row.Node is CollectionNode collection => new SelectorEditor(row, collection),
        EditorKind.List when row.Node is CollectionNode collection => new ListEditor(row, collection),
        _ => new TextEditor(row, display: false),
    };

    // Puts the control on its rectangle, with its content centred up and down.
    public virtual void Place(Rect bounds)
    {
        WpfRow.PlaceAt(Control, bounds);

        if (Control is Control control)
            control.VerticalContentAlignment = VerticalAlignment.Center;
    }

    public abstract void ShowValue();

    // Most editors are fields of their own color over the row's background, which shows the row under
    // the mouse by itself.
    public virtual void ShowHover(bool hovered)
    {
    }

    public virtual void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Control.IsEnabled = enabled && !readOnly;

        if (Control is Control control)
        {
            if (failed)
                control.Background = FailedBrush;
            else
                control.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
        }
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
