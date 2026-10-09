using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace InteractiveEditor.Avalonia;

// A button with the Text of a ButtonNode, which calls Press() (P7.9). An action that throws is a failure
// on the row, as the core reports it (P1.6).
internal sealed class ButtonEditor : AvaloniaEditor
{
    private readonly Button Push = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
    };

    private readonly TextBlock Caption = new();

    public ButtonEditor(AvaloniaRow row) : base(row)
    {
        // A text block, and not a string, so an underscore stays an underscore instead of marking a key.
        Push.Content = Caption;
        Push.Click += OnClick;
    }

    public override Control Control => Push;

    // The theme shows no error under it.
    public override bool ShowsErrors => false;

    public override void ShowValue()
    {
        Caption.Text = Node is ButtonNode button ? button.Text : Node.Label;
    }

    // Only a ButtonNode has an action to run.
    public override void ShowState(bool readOnly, bool enabled)
    {
        Push.IsEnabled = enabled && !readOnly && Node is ButtonNode;
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        if (Node is ButtonNode button)
            Row.Write(button.Press);
    }
}
