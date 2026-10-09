using Avalonia.Controls;

namespace InteractiveEditor.Avalonia;

// A horizontal line across the whole row (P7.9), the theme's separator.
internal sealed class SeparatorEditor : AvaloniaEditor
{
    private readonly Separator Line = new() { Focusable = false };

    public SeparatorEditor(AvaloniaRow row) : base(row)
    {
    }

    public override Control Control => Line;

    public override void ShowValue()
    {
    }

    public override void ShowState(bool readOnly, bool enabled)
    {
    }
}
