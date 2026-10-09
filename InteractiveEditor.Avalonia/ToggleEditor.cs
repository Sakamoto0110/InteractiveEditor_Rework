using Avalonia.Controls;
using Avalonia.Interactivity;

namespace InteractiveEditor.Avalonia;

// A check box for a bool (P7.9), written at once (P2.12). A nullable bool gets the third state, for null.
internal sealed class ToggleEditor : AvaloniaEditor
{
    private readonly CheckBox Box = new();

    public ToggleEditor(AvaloniaRow row) : base(row)
    {
        Box.IsThreeState = Node.ValueType is { } type && Nullable.GetUnderlyingType(type) == typeof(bool);
        Box.Click += OnClick;
    }

    public override Control Control => Box;

    // The theme shows no error under it.
    public override bool ShowsErrors => false;

    // Objects that hold different values leave the box indeterminate (P7.19); a click sets all of them.
    public override void ShowValue()
    {
        Box.IsChecked = Row.Mixed ? null : Node.ViewValue as bool?;
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        var value = Box.IsChecked;
        Row.Write(() => Node.SetValue(value));
        Row.Show();
    }
}
