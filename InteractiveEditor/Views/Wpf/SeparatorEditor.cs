using System.Windows;
using System.Windows.Controls;

namespace InteractiveEditor.Views.Wpf;

// A horizontal line across the whole row (P7.9): a dark line over a light one, as an etched edge.
internal sealed class SeparatorEditor : WpfEditor
{
    private readonly Border Line = new()
    {
        BorderBrush = SystemColors.ControlDarkBrush,
        BorderThickness = new Thickness(0, 1, 0, 0),
        Background = SystemColors.ControlLightLightBrush,
        Focusable = false,
    };

    public SeparatorEditor(WpfRow row) : base(row)
    {
    }

    public override FrameworkElement Control => Line;

    public override void Place(Rect bounds)
    {
        WpfRow.PlaceAt(Line, new Rect(bounds.X, bounds.Y + Math.Floor(bounds.Height / 2) - 1, bounds.Width, 2));
    }

    public override void ShowValue()
    {
    }

    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
    }
}
