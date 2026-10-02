using DemoObjects.ViewObjects;
using InteractiveEditor;
using InteractiveEditor.Events;
using InteractiveEditor.Views.WinForms;

namespace WindowsHost;

// The WinForms view of a Gadget, a member for each editor, with a button and a display added by hand,
// and a button that binds a second gadget, for a look at the values they do not share (P7.19). A valve
// (P7.4) aligns the numbers to the right, which the agnostic configuration does not do.
public partial class MainWindow : Form
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

        var view = inspector.CreateWinFormsView();
        view.ControlCreated += (_, e) =>
        {
            if (e is { Path: nameof(Gadget.Count) or nameof(Gadget.Ratio), Part: RowPart.Editor, Control: TextBox box })
                box.TextAlign = HorizontalAlignment.Right;
        };
        view.Dock = DockStyle.Fill;
        Controls.Add(view);

        FormClosed += (_, _) => inspector.Dispose();
    }
}
