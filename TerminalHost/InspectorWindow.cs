using System.Collections.ObjectModel;
using System.Text;
using InteractiveEditor;
using InteractiveEditor.Binding;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalHost;

internal sealed class InspectorWindow : Window
{
    private readonly ListView Targets;
    private readonly TreeView<InspectorNode> Tree;
    private readonly Label Info;
    private readonly TextField ValueField;
    private readonly Label EditStatus;
    private readonly ListView LogView;
    private readonly ObservableCollection<string> LogLines = [];

    private static readonly Rune NoHotKey = new(0xFFFF);

    private Inspector? Inspector;

    public InspectorWindow()
    {
        Title = "InteractiveEditor - Terminal host (F6 next panel, Esc quits)";

        var targetsFrame = new FrameView
        {
            Title = "Targets",
            X = 0,
            Y = 0,
            Width = Dim.Percent(40),
            Height = DemoTargets.All.Count + 2
        };

        Targets = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        Targets.SetSource(new ObservableCollection<string>(DemoTargets.All.Select(t => t.Name)));
        Targets.ValueChanged += (_, e) => LoadTarget(e.NewValue);
        targetsFrame.Add(Targets);

        var treeFrame = new FrameView
        {
            Title = "Tree",
            X = 0,
            Y = Pos.Bottom(targetsFrame),
            Width = Dim.Percent(40),
            Height = Dim.Fill(1)
        };

        Tree = new TreeView<InspectorNode>
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            TreeBuilder = new DelegateTreeBuilder<InspectorNode>(
                node => node is Inspector inspector ? inspector.Nodes : [],
                node => node is Inspector { Nodes.Count: > 0 }),
            AspectGetter = NodeInfo.Label
        };
        Tree.SelectionChanged += (_, e) => ShowNode(e.NewValue);
        treeFrame.Add(Tree);

        var infoFrame = new FrameView
        {
            Title = "Info",
            X = Pos.Right(targetsFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = 15,
            CanFocus = false
        };

        // Labels treat '_' as a hot key marker by default, which would eat the one in full paths (Foo_x).
        Info = new Label { Width = Dim.Fill(), Height = Dim.Fill(), HotKeySpecifier = NoHotKey };
        infoFrame.Add(Info);

        var editFrame = new FrameView
        {
            Title = "Edit (Enter applies; null clears nullable/reference members)",
            X = Pos.Right(targetsFrame),
            Y = Pos.Bottom(infoFrame),
            Width = Dim.Fill(),
            Height = 4
        };

        var valueLabel = new Label { Text = "Value:", X = 0, Y = 0 };
        var setButton = new Button { Text = "Set", X = Pos.AnchorEnd(), Y = 0 };
        ValueField = new TextField { X = Pos.Right(valueLabel) + 1, Y = 0, Width = Dim.Fill(setButton.Text.Length + 5) };
        EditStatus = new Label { X = 0, Y = 1, Width = Dim.Fill(), HotKeySpecifier = NoHotKey };

        ValueField.Accepting += (_, e) =>
        {
            ApplyEdit();
            e.Handled = true;
        };
        setButton.Accepting += (_, e) =>
        {
            ApplyEdit();
            e.Handled = true;
        };
        editFrame.Add(valueLabel, ValueField, setButton, EditStatus);

        var logFrame = new FrameView
        {
            Title = "Log",
            X = Pos.Right(targetsFrame),
            Y = Pos.Bottom(editFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        LogView = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        LogView.SetSource(LogLines);
        logFrame.Add(LogView);

        var statusBar = new StatusBar(
        [
            new Shortcut(Key.F5, "Refresh", Refresh, string.Empty),
            new Shortcut(Key.F3, "Check bindings", CheckBindings, string.Empty),
            new Shortcut(Key.F7, "Reset target", () => LoadTarget(Targets.Value), string.Empty),
        ]);

        Add(targetsFrame, treeFrame, infoFrame, editFrame, logFrame, statusBar);

        // Raises ValueChanged, which loads the first target.
        Targets.Value = 0;
    }

    private void LoadTarget(int? index)
    {
        if (index is not { } i || i < 0 || i >= DemoTargets.All.Count)
            return;

        var target = DemoTargets.All[i];

        Inspector = target.Create();

        Tree.ClearObjects();
        Tree.AddObject(Inspector);
        Tree.ExpandAll();
        Tree.SelectedObject = Inspector;

        ShowNode(Inspector);
        Log($"loaded {target.Name}: {Inspector.Count()} nodes");
    }

    private void ShowNode(InspectorNode? node)
    {
        EditStatus.Text = string.Empty;

        if (node == null)
        {
            Info.Text = string.Empty;
            ValueField.Text = string.Empty;
            return;
        }

        Info.Text = NodeInfo.Describe(node);
        ValueField.Text = node is Fieldset && NodeInfo.TryGetValue(node, out var value) ? NodeInfo.EditText(value) : string.Empty;
    }

    private void ApplyEdit()
    {
        if (Inspector == null || Tree.SelectedObject is not Fieldset field)
        {
            EditStatus.Text = "Select a field (leaf) to edit.";
            return;
        }

        var path = field.Descriptor?.FullPath ?? field.Name;

        try
        {
            var type = NodeInfo.MemberTypeOf(field)
                ?? throw new InvalidOperationException($"Could not resolve the type of '{field.Name}'.");

            var before = field.GetValue();
            var value = NodeInfo.Parse(ValueField.Text, type);

            field.SetValue(value);

            // Read back through the inspector and, independently, straight from the bound object.
            var result = BindingCheck.Check(Inspector.GetValue(), field);
            var reached = result.Error == null && BindingCheck.Same(result.Direct, value);
            var verdict = reached && result.Ok ? "OK" : "MISMATCH";

            EditStatus.Text = $"{verdict}: inspector {NodeInfo.Format(result.Bound)}, object {NodeInfo.Format(result.Direct)}";
            Log($"set {path}: {NodeInfo.Format(before)} -> {NodeInfo.Format(value)} | inspector {NodeInfo.Format(result.Bound)}, object {NodeInfo.Format(result.Direct)} {verdict}");
        }
        catch (Exception ex)
        {
            var error = NodeInfo.Unwrap(ex);

            EditStatus.Text = $"Error: {error.Message}";
            Log($"error {path}: {error.GetType().Name}: {error.Message}");
        }

        Tree.SetNeedsDraw();
        Info.Text = NodeInfo.Describe(field);
    }

    private void Refresh()
    {
        Tree.SetNeedsDraw();
        ShowNode(Tree.SelectedObject);
    }

    private void CheckBindings()
    {
        if (Inspector == null)
            return;

        var results = BindingCheck.Run(Inspector).ToList();
        var failed = results.Where(r => !r.Ok).ToList();

        foreach (var result in failed)
            Log($"  MISMATCH {Dump.Describe(result)}");

        Log($"bindings {Inspector.Name}: {results.Count - failed.Count}/{results.Count} ok");
    }

    private void Log(string line)
    {
        LogLines.Add($"{DateTime.Now:HH:mm:ss} {line}");
        LogView.Value = LogLines.Count - 1;
    }
}
