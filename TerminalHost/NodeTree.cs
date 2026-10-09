using InteractiveEditor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalHost;

// The tree of the window: the inspector at the top, and below it the rows a view shows (Rows): ignored
// and hidden nodes left out, siblings by Order, and members below a group only. A branch is open while
// its group is not Collapsed, the core's word for it, both ways: the tree opens and closes branches as
// the groups say, and writes what the user opened or closed into them.
internal sealed class NodeTree : TreeView<object>
{
    // The inspector at the top of the tree, or null.
    private Inspector? Shown;

    public NodeTree()
    {
        TreeBuilder = new DelegateTreeBuilder<object>(ChildrenOf, item => ChildrenOf(item).Any());
        AspectGetter = item => item switch
        {
            Inspector inspector => NodeInfo.Title(inspector),
            InspectorNode node => NodeInfo.Label(node),
            _ => string.Empty,
        };
    }

    // Shows another inspector, or none, with its groups open as they say and the inspector selected.
    public void Show(Inspector? inspector)
    {
        ClearObjects();
        Shown = inspector;

        if (inspector == null)
            return;

        AddObject(inspector);
        Expand(inspector);
        ShowCollapsed(inspector);
        SelectedObject = inspector;
    }

    // Reads the rows again when they may have changed (an option, a rule of visibility, the bind),
    // keeping the branches still there.
    public void Reload()
    {
        RebuildTree();

        if (Shown != null)
            ShowCollapsed(Shown);

        SetNeedsDraw();
    }

    // Opens and closes the branches below an item as their groups say.
    public void ShowCollapsed(object item)
    {
        foreach (var node in ChildrenOf(item).OfType<InspectorNode>().Where(CanExpand))
        {
            if (node.Collapsed)
            {
                if (IsExpanded(node))
                    Collapse(node);

                continue;
            }

            if (!IsExpanded(node))
                Expand(node);

            ShowCollapsed(node);
        }
    }

    // Every way of opening or closing a branch (keys, through commands this tree cannot wrap, and the
    // mouse) ends in a draw, so the groups on screen take what the tree shows right before it draws.
    protected override bool OnDrawingContent(DrawContext? context)
    {
        WriteCollapsed();
        return base.OnDrawingContent(context);
    }

    private static IEnumerable<object> ChildrenOf(object item)
    {
        return item switch
        {
            Inspector inspector => inspector.Rows.Where(row => row.Parent is { Parent: null }),
            InspectorNode { IsGroup: true } node => node.Rows.Where(row => row.Parent == node),
            _ => [],
        };
    }

    // Top-down, one group at a time: the group the user opened or closed takes it, and the groups below
    // one that opened show as they say, since a branch new on screen is closed only by default.
    private void WriteCollapsed()
    {
        while (Shown != null && FirstChanged(Shown) is { } node)
        {
            node.Collapsed = !IsExpanded(node);

            if (!node.Collapsed)
                ShowCollapsed(node);
        }
    }

    // The first group on screen whose branch is not as it says; what is below a closed branch is not on
    // screen, and keeps what it said.
    private InspectorNode? FirstChanged(object item)
    {
        foreach (var node in GetChildren(item).OfType<InspectorNode>().Where(CanExpand))
        {
            if (node.Collapsed == IsExpanded(node))
                return node;

            if (FirstChanged(node) is { } below)
                return below;
        }

        return null;
    }
}
