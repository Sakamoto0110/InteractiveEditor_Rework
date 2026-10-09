using Avalonia.Controls;
using Avalonia.Threading;
using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.ViewObjects;
using InteractiveEditor;
using InteractiveEditor.Avalonia;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using AttributedBoo = DemoObjects.AttributedObjects.Boo;
using StructBoo = DemoObjects.StructObjects.Boo;
using StructCoo = DemoObjects.StructObjects.Coo;

namespace AvaloniaHost;

// The Avalonia view of each demo object, chosen in the list on the left, made with Create<T>() and Bind.
// The Gadget, a member for each editor, has a button and a display added by hand, and a button that binds
// a second gadget, for a look at the values they do not share (P7.19). The status line says what the last
// edit wrote, or why it failed, from the inspector's own events.
public partial class MainWindow : Window
{
    private static readonly (string Name, Func<Inspector> Create)[] DemoTargets =
    [
        ("Gadget", CreateGadget),
        ("Foo", () => Bind(new Foo())),
        ("Moo", () => Bind(new Moo())),
        ("Doo", () => Bind(new Doo())),
        ("Boo (attributes)", () => Bind(new AttributedBoo())),
        ("Boo (struct)", () => Bind(new StructBoo { BooX = 1, BooY = 2, Coo = new StructCoo { CooX = 3, CooY = 4.5f } })),
        ("Coo (struct)", () => Bind(new StructCoo { CooX = 3, CooY = 4.5f })),
        ("Hoo", () => Bind(new Hoo())),
    ];

    // The inspector of the object chosen, and its view.
    private Inspector? Shown;
    private AvaloniaInspectorView? View;

    // Set from the first value a write reports until the UI thread is free again: a write reports the node
    // written first, and then whatever changed with it (the struct above it, a collection).
    private bool Reported;

    public MainWindow()
    {
        InitializeComponent();

        Targets.ItemsSource = DemoTargets.Select(t => t.Name).ToList();
        Targets.SelectionChanged += (_, _) => LoadTarget(Targets.SelectedIndex);
        RefreshButton.Click += (_, _) => Shown?.Refresh();
        Closed += (_, _) => Unload();

        Targets.SelectedIndex = 0;
    }

    private void LoadTarget(int index)
    {
        if (index < 0)
            return;

        Unload();

        var (name, create) = DemoTargets[index];
        Shown = create();

        foreach (var node in Shown)
        {
            node.ValueChanged += OnValueChanged;
            node.BindFailed += OnBindFailed;
        }

        View = Shown.CreateAvaloniaView();
        InspectorHost.Content = View;
        Status.Text = $"Loaded {name}";
    }

    // The view lets the inspector go, and the inspector lets its objects and the global options go.
    private void Unload()
    {
        InspectorHost.Content = null;
        View?.Dispose();
        Shown?.Dispose();
        View = null;
        Shown = null;
    }

    private void OnValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (e.Source != ValueSource.Write || Reported)
            return;

        Reported = true;
        Dispatcher.UIThread.Post(() => Reported = false);

        var value = e.Node is CollectionNode collection ? $"{collection.Items.Count} items" : e.Node.ViewValue ?? "null";
        Status.Text = $"{e.Node.Path} = {value}";
    }

    private void OnBindFailed(object? sender, InspectorFailureEventArgs e)
    {
        Status.Text = $"{e.Path}: {e.Message} {e.Reason}";
    }

    private static Inspector Bind<T>(T instance)
        where T : notnull
    {
        var inspector = Inspector.Create<T>();
        inspector.Bind(instance);
        return inspector;
    }

    // As the WPF host shows it: a button and a display added by hand, and a second gadget to bind.
    private static Inspector CreateGadget()
    {
        var gadget = new Gadget();
        var second = new Gadget { Name = "second", Count = 7, Ratio = 0.25, Enabled = false, Mode = GadgetMode.Fast };
        var inspector = Inspector.Create<Gadget>();

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
        return inspector;
    }
}
