using System.Windows;
using System.Windows.Controls;
using InteractiveEditor.Options;

namespace InteractiveEditor.Views.Wpf;

// A slider over the node's range (P7.9). A WPF slider counts in double, so it takes the range as it is,
// stopping on the step when there is one, and it writes while it is dragged (P2.12).
internal sealed class SliderEditor : WpfEditor
{
    private readonly Slider Bar = new() { Focusable = true };

    // Set while the view moves the slider, so only the user's move writes.
    private bool Showing;

    public SliderEditor(WpfRow row) : base(row)
    {
        Bar.ValueChanged += OnValueChanged;
    }

    public override FrameworkElement Control => Bar;

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
        }
        finally
        {
            Showing = false;
        }
    }

    private void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Showing)
            return;

        var value = e.NewValue;
        Row.Write(() => Node.SetValue(value));
    }
}
