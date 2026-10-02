using System.Windows;
using System.Windows.Controls;

namespace InteractiveEditor.Views.Wpf;

// A button with the Text of a ButtonNode, which calls Press() (P7.9). An action that throws is a failure
// on the row, as the core reports it (P1.6).
internal sealed class ButtonEditor : WpfEditor
{
    private readonly Button Push = new();
    private readonly TextBlock Caption = new();

    public ButtonEditor(WpfRow row) : base(row)
    {
        // A text block, and not a string, so an underscore stays an underscore.
        Push.Content = Caption;
        Push.Click += OnClick;
    }

    public override FrameworkElement Control => Push;

    public override void ShowValue()
    {
        Caption.Text = Node is ButtonNode button ? button.Text : Node.Label;
    }

    // Only a ButtonNode has an action to run.
    public override void ShowState(bool readOnly, bool enabled, bool failed)
    {
        base.ShowState(readOnly, enabled, failed);
        Push.IsEnabled = enabled && !readOnly && Node is ButtonNode;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (Node is ButtonNode button)
            Row.Write(button.Press);
    }
}
