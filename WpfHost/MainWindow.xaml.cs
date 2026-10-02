using System.Windows;
using System.Windows.Controls;
using DemoObjects.ViewObjects;
using InteractiveEditor;
using InteractiveEditor.Events;
using InteractiveEditor.Views.Wpf;

namespace WpfHost;

// The WPF view of a Gadget, as the WindowsHost shows the WinForms one: a member for each editor, with a
// button and a display added by hand, a button that binds a second gadget, for a look at the values they
// do not share (P7.19), and a valve (P7.4) that aligns the numbers to the right.
public partial class MainWindow : Window
{
    private readonly Gadget gadget = new();
    private readonly Gadget second = new() { Name = "second", Count = 7, Ratio = 0.25, Enabled = false, Mode = GadgetMode.Fast };
    private readonly Inspector inspector = Inspector.Create<Gadget>();

    public MainWindow()
    {
        InitializeComponent();

        inspector.AddButton("Refresh", "Read the object again", inspector.Refresh);
        inspector.AddDisplay("Total", () => gadget.Levels.Sum());

        ButtonNode? both = null;
        both = inspector.AddButton("Second", "Bind a second gadget", () =>
        {
            if (inspector.Instances.Count > 1)
            {
                inspector.RemoveBind(second);
                both!.Text = "Bind a second gadget";
            }
            else
            {
                inspector.AddBind(second);
                both!.Text = "Unbind the second gadget";
            }
        });

        inspector.Bind(gadget);

        var view = inspector.CreateWpfView();
        view.ControlCreated += (_, e) =>
        {
            if (e is { Path: nameof(Gadget.Count) or nameof(Gadget.Ratio), Part: RowPart.Editor, Control: TextBox box })
                box.TextAlignment = TextAlignment.Right;
        };
        Content = view;

        Closed += (_, _) =>
        {
            view.Dispose();
            inspector.Dispose();
        };
    }
}
