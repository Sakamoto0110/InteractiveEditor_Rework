using DemoObjects.ViewObjects;
using InteractiveEditor;
using InteractiveEditor.Views.WinForms;

namespace WindowsHost;

// The WinForms view of a Gadget, a member for each editor, with a button and a display added by hand.
public partial class MainWindow : Form
{
    private readonly Gadget gadget = new();
    private readonly Inspector inspector = Inspector.Create<Gadget>();

    public MainWindow()
    {
        InitializeComponent();

        inspector.AddButton("Refresh", "Read the object again", inspector.Refresh);
        inspector.AddDisplay("Total", () => gadget.Levels.Sum());
        inspector.Bind(gadget);

        var view = inspector.CreateWinFormsView();
        view.Dock = DockStyle.Fill;
        Controls.Add(view);

        FormClosed += (_, _) => inspector.Dispose();
    }
}
