using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace InteractiveEditor.Avalonia;

// A value shown and not edited (P7.9): a value added by hand (AddDisplay), or a member whose type has no
// editor. The text can be selected and copied. As the property grid always showed it, null reads "null"
// and a text goes in quotes, so an empty text and null look different; objects that hold different values
// show a grey dash (P7.19).
internal sealed class DisplayEditor : AvaloniaEditor
{
    private readonly SelectableTextBlock Block = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    public DisplayEditor(AvaloniaRow row) : base(row)
    {
    }

    public override Control Control => Block;

    // The theme shows no error under it.
    public override bool ShowsErrors => false;

    public override void ShowValue()
    {
        var mixed = Row.Mixed;
        var value = Node.ViewValue;

        Block.Text = mixed ? "—" : value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            _ => Format(value),
        };
        Block.Opacity = mixed ? 0.6 : 1;
    }

    // A display is read-only by nature, so only a disabled branch greys it out.
    public override void ShowState(bool readOnly, bool enabled)
    {
        Block.IsEnabled = enabled;
    }
}
