using System.Drawing;
using System.Windows.Forms;
using InteractiveEditor.Options;

namespace InteractiveEditor.Views.WinForms;

// The control of one editor (P7.9), shown and written through its row.
internal abstract class WinFormsEditor : IDisposable
{
    // The background of an editor whose node failed (P7.11).
    protected static readonly Color FailedColor = Color.FromArgb(255, 224, 224);

    protected WinFormsEditor(WinFormsRow row)
    {
        Row = row;
    }

    public abstract Control Control { get; }

    protected WinFormsRow Row { get; }

    protected InspectorNode Node => Row.Node;

    // The editor of each kind (P7.9); a header has none, and a selector or a list on something that is
    // not a collection falls back to text.
    public static WinFormsEditor? For(WinFormsRow row, EditorKind? kind) => kind switch
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

    // Puts the control on its rectangle, centred in it when the control keeps a height of its own, as a
    // text box or a combo box does by its font.
    public virtual void Place(Rectangle bounds)
    {
        Control.Bounds = bounds;

        if (Control.Height != bounds.Height)
            Control.Top = bounds.Top + (bounds.Height - Control.Height) / 2;
    }

    public abstract void ShowValue();

    public virtual void ShowState(bool readOnly, bool enabled, bool failed)
    {
        Control.Enabled = enabled && !readOnly;

        if (failed)
            Control.BackColor = FailedColor;
        else
            Control.ResetBackColor();
    }

    public virtual void Dispose()
    {
        Control.Dispose();
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
