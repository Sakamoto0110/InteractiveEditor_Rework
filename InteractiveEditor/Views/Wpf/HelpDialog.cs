using System.Windows;
using System.Windows.Controls;

namespace InteractiveEditor.Views.Wpf;

// The long help of a node (P7.15): a modal window, titled with the row's label, that blocks the one
// behind it until it closes. The text scrolls and can be selected; OK, Enter and Esc close it.
internal sealed class HelpDialog : Window
{
    public HelpDialog(string title, string text)
    {
        Title = title;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.CanResize;
        Width = 420;
        Height = 260;
        MinWidth = 240;
        MinHeight = 160;

        var body = new TextBox
        {
            Text = text,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(8, 8, 8, 0),
        };
        WpfInspectorView.AutomationId(body, "text");

        var ok = new Button { Content = "OK", IsDefault = true, IsCancel = true, MinWidth = 75, Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Right };
        WpfInspectorView.AutomationId(ok, "ok");
        ok.Click += (_, _) => DialogResult = true;

        var panel = new DockPanel();
        DockPanel.SetDock(ok, Dock.Bottom);
        panel.Children.Add(ok);
        panel.Children.Add(body);
        Content = panel;

        Loaded += (_, _) => ok.Focus();
    }
}
