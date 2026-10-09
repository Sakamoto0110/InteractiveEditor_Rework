using Avalonia.Controls;
using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.StructObjects;
using InteractiveEditor;
using InteractiveEditor.Avalonia;

namespace AvaloniaHost;

public partial class MainWindow : Window
{
    private static readonly (string Name, Func<Inspector> Create)[] DemoTargets =
    [
        ("Foo", () => Bind(new Foo())),
        ("Moo", () => Bind(new Moo())),
        ("Hoo", () => Bind(new Hoo())),
        ("Boo", () => Bind(new Boo { BooX = 1, BooY = 2, Coo = new Coo { CooX = 3, CooY = 4.5f } })),
    ];

    private InspectorView? View;

    public MainWindow()
    {
        InitializeComponent();

        Targets.ItemsSource = DemoTargets.Select(t => t.Name).ToList();
        Targets.SelectionChanged += (_, _) => LoadTarget(Targets.SelectedIndex);
        RefreshButton.Click += (_, _) => View?.Refresh();

        Targets.SelectedIndex = 0;
    }

    private void LoadTarget(int index)
    {
        if (index < 0)
            return;

        var (name, create) = DemoTargets[index];

        View = new InspectorView(create(), InspectorHost);
        View.FieldEdited += (_, e) =>
        {
            var path = e.Field.Descriptor?.FullPath ?? e.Field.Name;

            Status.Text = e.Error == null
                ? $"{path} = {e.Field.GetValue() ?? "null"}"
                : $"{path}: {e.Error.Message}";
        };

        Status.Text = $"Loaded {name}";
    }

    private static Inspector Bind<T>(T instance)
    {
        var inspector = Inspector.Create<T>();
        inspector.bind(instance);
        return inspector;
    }
}
