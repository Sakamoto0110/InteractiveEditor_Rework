using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using InteractiveEditor;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.Options;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalHost;

// The window is the view of an inspector: the tree of its rows, the Info of the one selected, an edit box
// that writes it through the core (SetValue with the text typed, Press, the item of a collection), and a
// log. It owns the inspector of the target it loads, and shows whatever the inspector reports (a value,
// an option, the bind, a failure) as it comes.
internal sealed class InspectorWindow : Window
{
    private static readonly Rune NoHotKey = new(0xFFFF);

    private readonly ListView Targets;
    private readonly NodeTree Tree;
    private readonly Label Info;
    private readonly TextField ValueField;
    private readonly Button SetButton;
    private readonly Label Hint;
    private readonly Button AddButton;
    private readonly Button RemoveButton;
    private readonly Button UpButton;
    private readonly Button DownButton;
    private readonly Label EditStatus;
    private readonly ListView LogView;
    private readonly ObservableCollection<string> LogLines = [];
    private readonly HashSet<InspectorNode> Watched = [];

    private Inspector? Inspector;

    // The text the edit box got from the row last; while the box holds something else, the user is
    // typing, and a new value of the row waits for the selection to change.
    private string ShownText = string.Empty;

    // The rows a Refresh() found changed.
    private int Refreshed;

    // Set while the selection shows. Showing can itself fail on the node (a Choices function that throws
    // when the hint lists the choices), and that failure comes back through BindFailed.
    private bool Showing;

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

        Tree = new NodeTree { Width = Dim.Fill(), Height = Dim.Fill() };
        Tree.SelectionChanged += (_, _) => ShowSelection(keepTyping: false);
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

        // Labels treat '_' as a hot key marker by default, which would eat the one in paths (Foo_x).
        Info = new Label { Width = Dim.Fill(), Height = Dim.Fill(), HotKeySpecifier = NoHotKey };
        infoFrame.Add(Info);

        var editFrame = new FrameView
        {
            Title = "Edit (Enter applies; null clears nullable/reference members)",
            X = Pos.Right(targetsFrame),
            Y = Pos.Bottom(infoFrame),
            Width = Dim.Fill(),
            Height = 5
        };

        // The buttons go without the shadow Terminal.Gui gives them, which would cover the line below.
        var valueLabel = new Label { Text = "Value:", X = 0, Y = 0 };
        SetButton = new Button { Text = "Set", X = Pos.AnchorEnd(), Y = 0, ShadowStyle = ShadowStyles.None };
        ValueField = new TextField { X = Pos.Right(valueLabel) + 1, Y = 0, Width = Dim.Fill(12) };

        // The buttons of a List editor (P5.10), on the line of the hint, which they push right.
        AddButton = new Button { Text = "Add", X = 0, Y = 1, Visible = false, ShadowStyle = ShadowStyles.None };
        RemoveButton = new Button { Text = "Remove", X = Pos.Right(AddButton), Y = 1, Visible = false, ShadowStyle = ShadowStyles.None };
        UpButton = new Button { Text = "Up", X = Pos.Right(RemoveButton), Y = 1, Visible = false, ShadowStyle = ShadowStyles.None };
        DownButton = new Button { Text = "Down", X = Pos.Right(UpButton), Y = 1, Visible = false, ShadowStyle = ShadowStyles.None };
        Hint = new Label { X = 0, Y = 1, Width = Dim.Fill(), HotKeySpecifier = NoHotKey };
        EditStatus = new Label { X = 0, Y = 2, Width = Dim.Fill(), HotKeySpecifier = NoHotKey };

        ValueField.Accepting += (_, e) =>
        {
            Commit();
            e.Handled = true;
        };
        SetButton.Accepting += (_, e) =>
        {
            Commit();
            e.Handled = true;
        };
        AddButton.Accepting += (_, e) =>
        {
            ChangeItems("add", collection => collection.AddItem());
            e.Handled = true;
        };
        RemoveButton.Accepting += (_, e) =>
        {
            ChangeItems("remove", collection => collection.RemoveItem(collection.SelectedIndex));
            e.Handled = true;
        };
        UpButton.Accepting += (_, e) =>
        {
            ChangeItems("move up", collection => collection.MoveItem(collection.SelectedIndex, collection.SelectedIndex - 1));
            e.Handled = true;
        };
        DownButton.Accepting += (_, e) =>
        {
            ChangeItems("move down", collection => collection.MoveItem(collection.SelectedIndex, collection.SelectedIndex + 1));
            e.Handled = true;
        };
        editFrame.Add(valueLabel, ValueField, SetButton, Hint, AddButton, RemoveButton, UpButton, DownButton, EditStatus);

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Release();

        base.Dispose(disposing);
    }

    private InspectorNode? SelectedNode => Tree.SelectedObject as InspectorNode;

    private void LoadTarget(int? index)
    {
        if (index is not { } i || i < 0 || i >= DemoTargets.All.Count)
            return;

        var target = DemoTargets.All[i];

        Release();
        Inspector = target.Create();
        Watch(Inspector);

        Tree.Show(Inspector);
        ShowSelection(keepTyping: false);

        foreach (var failure in Inspector.Report.Failures)
            Log($"create {failure.Path}: {failure.Message} {failure.Reason}");

        Log($"loaded {target.Name}: {Inspector.Count()} nodes, {Inspector.Rows.Count()} rows");
    }

    // The Info of the selection, and the edit panel for what it is. With keepTyping, a box that holds
    // an edit of its own keeps it.
    private void ShowSelection(bool keepTyping)
    {
        if (Showing)
            return;

        Showing = true;

        try
        {
            ShowSelection(SelectedNode, keepTyping);
        }
        finally
        {
            Showing = false;
        }
    }

    private void ShowSelection(InspectorNode? node, bool keepTyping)
    {
        var action = node == null ? EditAction.None : EditActions.For(node);
        var enabled = node is { IsCompromised: false };
        var writable = node is { IsCompromised: false, ReadOnly: false };

        Info.Text = Tree.SelectedObject switch
        {
            Inspector inspector => NodeInfo.Describe(inspector),
            InspectorNode selected => NodeInfo.Describe(selected),
            _ => string.Empty,
        };

        var text = node == null ? string.Empty : action switch
        {
            EditAction.Value or EditAction.Display => EditActions.ShowsMixed(node) ? string.Empty : NodeText.EditText(node, node.ViewValue),
            EditAction.Press => ((ButtonNode)node).Text,
            EditAction.Choose => ((CollectionNode)node).SelectedIndex.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty,
        };

        if (!keepTyping || ValueField.Text == ShownText)
        {
            ValueField.Text = text;
            ShownText = text;
        }

        if (!keepTyping)
            EditStatus.Text = string.Empty;

        // Choosing an item is not an edit, so a read-only collection still chooses which one shows.
        ValueField.ReadOnly = !(action == EditAction.Value && writable || action == EditAction.Choose && enabled);
        SetButton.Text = action switch
        {
            EditAction.Press => "Press",
            EditAction.Choose => "Choose",
            _ => "Set",
        };
        SetButton.Enabled = action switch
        {
            EditAction.Value or EditAction.Press => writable,
            EditAction.Choose => enabled,
            _ => false,
        };

        ShowListButtons(node, action == EditAction.Choose && node is { Editor: EditorKind.List }, writable);
        Hint.Text = node == null && Tree.SelectedObject is Inspector ? "The inspector: select a row to edit it." : NodeInfo.Hint(node);
    }

    private void ShowListButtons(InspectorNode? node, bool shown, bool writable)
    {
        var index = (node as CollectionNode)?.SelectedIndex ?? -1;
        var count = (node as CollectionNode)?.Items.Count ?? 0;

        foreach (var button in new[] { AddButton, RemoveButton, UpButton, DownButton })
            button.Visible = shown;

        AddButton.Enabled = writable;
        RemoveButton.Enabled = writable && index >= 0;
        UpButton.Enabled = writable && index > 0;
        DownButton.Enabled = writable && index >= 0 && index < count - 1;
        Hint.X = shown ? Pos.Right(DownButton) + 1 : 0;
    }

    // Enter in the edit box, or the button next to it: the value typed, a press, or the item chosen.
    private void Commit()
    {
        if (SelectedNode is not { } node)
        {
            EditStatus.Text = "Select a row to edit it.";
            return;
        }

        switch (EditActions.For(node))
        {
            case EditAction.Value:
                SetValue(node);
                break;

            case EditAction.Press:
                if (TryWrite(node, ((ButtonNode)node).Press))
                    Done(node, $"pressed {node.Path}", checkFailure: true);
                break;

            case EditAction.Choose:
                Choose((CollectionNode)node);
                break;

            default:
                EditStatus.Text = NodeInfo.Hint(node);
                break;
        }
    }

    // The core converts the text with its own rules (P2.7). A text it refuses is a failure of the node,
    // reported with BindFailed, and the box keeps what was typed; once the write goes through, it is read
    // back from the object with plain reflection.
    private void SetValue(InspectorNode node)
    {
        var typed = ValueField.Text;
        var before = EditActions.ShowsMixed(node) ? "<mixed>" : NodeText.Format(node, node.ViewValue);

        if (!NodeText.TryValueFor(node, typed, out var value))
        {
            EditStatus.Text = $"Error: '{typed}' is not a color. {NodeInfo.Hint(node)}";
            return;
        }

        if (!TryWrite(node, () => node.SetValue(value)))
            return;

        if (node.Failure is { } failure)
        {
            EditStatus.Text = $"Error: {failure.Reason}";
            return;
        }

        var result = BindingCheck.Check(node);
        var reached = result.Error == null && result.Direct.Count == node.ViewValues.Count
            && result.Direct.Zip(node.ViewValues).All(pair => BindingCheck.Same(pair.First, pair.Second));
        var verdict = reached && result.Ok ? "OK" : "MISMATCH";
        var bound = NodeText.Format(node, result.Bound);
        var direct = NodeText.Format(node, result.Direct);

        ShowSelection(keepTyping: false);
        EditStatus.Text = $"{verdict}: inspector {bound}, object {direct}";
        Log($"set {node.Path}: {before} -> {NodeText.Format(node, node.ViewValue)} | inspector {bound}, object {direct} {verdict}");
    }

    private void Choose(CollectionNode collection)
    {
        if (!int.TryParse(ValueField.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            EditStatus.Text = "Error: type the index of an item, or -1 for none.";
            return;
        }

        if (TryWrite(collection, () => collection.SelectedIndex = index))
            Done(collection, $"chose {collection.Path}: #{index}", checkFailure: false);
    }

    // The operations of a List editor write at once, whatever the binder control (P5.13).
    private void ChangeItems(string what, Action<CollectionNode> change)
    {
        if (SelectedNode is not CollectionNode collection)
            return;

        if (TryWrite(collection, () => change(collection)))
            Done(collection, $"{what} {collection.Path}: {collection.Items.Count} items", checkFailure: true);
    }

    // An action that throws, and a list that refuses an operation, do not throw: the core keeps the
    // failure on the node (and BindFailed logs it), and clears it when one goes through. Choosing an item
    // does not clear it, so a failure there is an older one.
    private void Done(InspectorNode node, string line, bool checkFailure)
    {
        ShowSelection(keepTyping: false);

        if (checkFailure && node.Failure is { } failure)
        {
            EditStatus.Text = $"Error: {failure.Reason}";
            return;
        }

        EditStatus.Text = $"OK: {NodeInfo.Label(node)}";
        Log(line);
    }

    // Runs a write through the core: what it refuses as a mistake (a read-only node, a group, a branch
    // disabled by a replaced group, an index past the items) shows on the panel and in the log instead
    // of bringing the window down.
    private bool TryWrite(InspectorNode node, Action write)
    {
        try
        {
            write();
            return true;
        }
        catch (Exception ex)
        {
            var error = NodeText.Unwrap(ex);

            EditStatus.Text = $"Error: {error.Message}";
            Log($"error {node.Path}: {error.GetType().Name}: {error.Message}");
            return false;
        }
    }

    // Reads the objects again; the rows that changed come back through ValueChanged.
    private void Refresh()
    {
        if (Inspector == null)
            return;

        Refreshed = 0;
        Inspector.Refresh();
        Log($"refresh {Inspector.Name}: {Refreshed} rows changed");
    }

    private void CheckBindings()
    {
        if (Inspector == null)
            return;

        var results = BindingCheck.Run(Inspector).ToList();
        var failed = results.Where(r => !r.Ok).ToList();

        foreach (var result in failed)
            Log($"  MISMATCH {result.Describe()}");

        Log($"bindings {Inspector.Name}: {results.Count - failed.Count}/{results.Count} ok");
    }

    private void Log(string line)
    {
        LogLines.Add($"{DateTime.Now:HH:mm:ss} {line}");
        LogView.Value = LogLines.Count - 1;
    }

    // Every node of the tree, also the hidden ones, whose rule can show them again; nodes added by hand
    // later are taken at the next reload.
    private void Watch(Inspector inspector)
    {
        inspector.OptionChanged += OnOptionChanged;
        inspector.BindRegistered += OnBindChanged;
        inspector.BindRemoved += OnBindChanged;
        inspector.Unbound += OnBindChanged;
        inspector.ForcedApply += OnBindChanged;
        inspector.ForcedReload += OnBindChanged;
        inspector.ForcedClear += OnBindChanged;
        inspector.Disposed += OnInspectorDisposed;
        WatchNodes(inspector);
    }

    private void WatchNodes(Inspector inspector)
    {
        foreach (var node in inspector)
        {
            if (!Watched.Add(node))
                continue;

            node.ValueChanged += OnValueChanged;
            node.BindFailed += OnBindFailed;
            node.VisibleChanged += OnVisibleChanged;
            node.ObjectReplaced += OnObjectReplaced;
        }
    }

    // Lets the inspector go: no more events from it, and it is disposed, which releases its hold on the
    // global options.
    private void Release()
    {
        if (Inspector is not { } inspector)
            return;

        inspector.OptionChanged -= OnOptionChanged;
        inspector.BindRegistered -= OnBindChanged;
        inspector.BindRemoved -= OnBindChanged;
        inspector.Unbound -= OnBindChanged;
        inspector.ForcedApply -= OnBindChanged;
        inspector.ForcedReload -= OnBindChanged;
        inspector.ForcedClear -= OnBindChanged;
        inspector.Disposed -= OnInspectorDisposed;

        foreach (var node in Watched)
        {
            node.ValueChanged -= OnValueChanged;
            node.BindFailed -= OnBindFailed;
            node.VisibleChanged -= OnVisibleChanged;
            node.ObjectReplaced -= OnObjectReplaced;
        }

        Watched.Clear();
        Inspector = null;
        inspector.Dispose();
    }

    // The rows may have changed: read them again, and move the selection off a row that left.
    private void Reload()
    {
        if (Inspector == null)
            return;

        WatchNodes(Inspector);
        Tree.Reload();

        if (SelectedNode is { } node && !Inspector.Rows.Contains(node))
            Tree.SelectedObject = Inspector;

        ShowSelection(keepTyping: true);
    }

    // Collapsed only opens or closes a branch; any other option can change the rows.
    private void OnOptionChanged(object? sender, OptionChangedEventArgs e)
    {
        if (e.Option == nameof(InspectorNode.Collapsed) && Inspector != null)
        {
            Tree.ShowCollapsed(Inspector);
            ShowSelection(keepTyping: true);
            return;
        }

        Reload();
    }

    // A bind starts every node over in silence, and the forced operations can change any row.
    private void OnBindChanged(object? sender, EventArgs e)
    {
        Reload();
    }

    private void OnVisibleChanged(object? sender, VisibleChangedEventArgs e)
    {
        Reload();
    }

    private void OnObjectReplaced(object? sender, ObjectReplacedEventArgs e)
    {
        Log($"replaced {e.Node.Path}: the branch is disabled until a Rebind");
        Reload();
    }

    private void OnValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (e.Source == ValueSource.Refresh)
            Refreshed++;

        Tree.SetNeedsDraw();

        if (e.Node == SelectedNode)
            ShowSelection(keepTyping: true);
    }

    private void OnBindFailed(object? sender, InspectorFailureEventArgs e)
    {
        Log($"failed {e.Path}: {e.Message} {e.Reason}");
        Tree.SetNeedsDraw();

        if (sender == SelectedNode)
            ShowSelection(keepTyping: true);
    }

    private void OnInspectorDisposed(object? sender, InspectorEventArgs e)
    {
        Release();
        Tree.Show(null);
        ShowSelection(keepTyping: false);
    }
}
