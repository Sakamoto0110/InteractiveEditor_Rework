using System.Windows;
using System.Windows.Controls;

namespace InteractiveEditor.Views.Wpf;

// A check box for a bool (P7.9), written at once (P2.12).
internal sealed class ToggleEditor : WpfEditor
{
    private readonly CheckBox Box = new() { IsThreeState = false };

    public ToggleEditor(WpfRow row) : base(row)
    {
        Box.Click += OnClick;
    }

    public override FrameworkElement Control => Box;

    // Objects that hold different values leave the box indeterminate (P7.19); a click sets all of them.
    public override void ShowValue()
    {
        Box.IsChecked = Row.Mixed ? null : Node.ViewValue is true;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        var value = Box.IsChecked == true;
        Row.Write(() => Node.SetValue(value));
        Row.Show();
    }
}
