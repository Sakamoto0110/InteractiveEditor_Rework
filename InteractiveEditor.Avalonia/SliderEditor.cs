using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using InteractiveEditor.Views;

namespace InteractiveEditor.Avalonia;

// A slider over the node's range (P7.9), with the value as text beside it, since the slider alone does not
// say it. An Avalonia slider counts in double, so it takes the range as it is, stopping on the step when
// there is one, and it writes while it is dragged (P2.12).
internal sealed class SliderEditor : AvaloniaEditor
{
    private readonly DockPanel Frame = new();
    private readonly Slider Bar = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock Number = new()
    {
        MinWidth = 40,
        Margin = new Thickness(8, 0, 0, 0),
        TextAlignment = TextAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center,
    };

    // Set while the view moves the slider, so only the user's move writes.
    private bool Showing;

    public SliderEditor(AvaloniaRow row) : base(row)
    {
        DockPanel.SetDock(Number, Dock.Right);
        Frame.Children.Add(Number);
        Frame.Children.Add(Bar);
        Bar.ValueChanged += OnValueChanged;
    }

    public override Control Control => Frame;

    public override Control ErrorTarget => Bar;

    // Objects that hold different values leave the slider where the first one is, and the text says they
    // differ (P7.19).
    public override void ShowValue()
    {
        var range = Node.Range ?? default;
        var span = Math.Max(0, range.Max - range.Min);
        Showing = true;

        try
        {
            Bar.Minimum = range.Min;
            Bar.Maximum = range.Min + span;
            Bar.IsSnapToTickEnabled = range.Step > 0;
            Bar.TickFrequency = range.Step > 0 ? range.Step : span / 100;
            Bar.SmallChange = Bar.TickFrequency;
            Bar.LargeChange = Math.Max(Bar.TickFrequency, span / 10);
            Bar.Value = Math.Clamp(ViewRules.ToDouble(Node.ViewValue, range.Min), Bar.Minimum, Bar.Maximum);
            Number.Text = Row.Mixed ? "—" : Format(Node.ViewValue);
        }
        finally
        {
            Showing = false;
        }
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (Showing)
            return;

        var value = e.NewValue;
        Row.Write(() => Node.SetValue(value));
    }
}
