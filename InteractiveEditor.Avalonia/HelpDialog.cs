using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace InteractiveEditor.Avalonia;

// The long help of a node (P7.15): a modal window, titled with the row's label, that blocks the one
// behind it until it closes. The text scrolls and can be selected; OK, Enter and Esc close it.
internal sealed class HelpDialog : Window
{
    public HelpDialog(string title, string text)
    {
        Title = title;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        CanResize = true;
        Width = 420;
        Height = 260;
        MinWidth = 240;
        MinHeight = 160;

        var body = new TextBox
        {
            Text = text,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 8, 8, 0),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(body, ScrollBarVisibility.Auto);
        AutomationProperties.SetAutomationId(body, "text");

        var ok = new Button
        {
            Content = "OK",
            IsDefault = true,
            IsCancel = true,
            MinWidth = 75,
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Right,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(ok, "ok");
        ok.Click += (_, _) => Close(true);

        var panel = new DockPanel();
        DockPanel.SetDock(ok, Dock.Bottom);
        panel.Children.Add(ok);
        panel.Children.Add(body);
        Content = panel;

        Opened += (_, _) => ok.Focus();
    }
}
