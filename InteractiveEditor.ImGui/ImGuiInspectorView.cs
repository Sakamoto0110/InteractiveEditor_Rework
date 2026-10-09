using System.Numerics;
using ImGuiNET;
using InteractiveEditor.Events;
// Inside InteractiveEditor.ImGui the bare name ImGui resolves to this namespace, hence the alias.
using Gui = ImGuiNET.ImGui;

namespace InteractiveEditor.ImGui;

// The Dear ImGui view of an inspector: a two-column table, the labels on the left and the editors on the
// right, where groups are tree nodes. Being immediate mode, it walks the inspector's rows on every frame
// and shows what each node holds then, so a change of value, option, visibility or bind shows at the next
// frame without following the events of the nodes; it keeps only what ImGui does not (the text being
// typed, a drag). It does not need the layout step: ImGui places the widgets itself. Disposing the view
// leaves the inspector alone; when the inspector is disposed, the view empties (P7.13). An exception of a
// subscriber goes up through the view, as one of the inspector's events does (3.11), with ImGui's Begin
// and End pairs closed on the way, so a host that catches it can go on with the frame.
public sealed class ImGuiInspectorView : IDisposable
{
    // The text of a value that cannot be edited, and of the summary of a group.
    internal static readonly Vector4 DisabledColor = new(0.6f, 0.6f, 0.6f, 1f);

    // The text of a failure, under the editor of its row (P7.11).
    internal static readonly Vector4 ErrorColor = new(1f, 0.4f, 0.4f, 1f);

    // The rows drawn so far, kept for what ImGui does not keep between frames; a node that changes its
    // editor gets a new row.
    private readonly Dictionary<InspectorNode, ImGuiRow> RowsByNode = [];

    private IImGuiHost? Host;
    private InspectorNode? PendingFocus;

    internal ImGuiInspectorView(Inspector inspector)
    {
        Inspector = inspector;
        Title = inspector.Name;
        inspector.Disposed += OnInspectorDisposed;
    }

    // The inspector shown; null once it was disposed.
    public Inspector? Inspector { get; private set; }

    // Title of the window DrawWindow opens. It is also the window's ImGui ID: "Label###id" keeps the ID fixed.
    public string Title { get; set; }

    // The view draws every frame anyway, so by default it reads the objects every frame too (Refresh()),
    // and a change made outside the inspector shows without a call, as it did when the view read the
    // members itself. A host whose objects report their changes, or that calls Refresh() on its own
    // schedule, turns it off. Like Refresh(), it follows the binder control.
    public bool RefreshEachFrame { get; set; } = true;

    // Draws the view's window on every frame of the host.
    public void AttachToHost(IImGuiHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        Detach();
        Host = host;
        Host.Frame += DrawWindow;
    }

    public void Detach()
    {
        if (Host == null)
            return;

        Host.Frame -= DrawWindow;
        Host = null;
    }

    // Gives keyboard focus to a node's editor on the next frame, opening the groups above it.
    public void Focus(InspectorNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (Inspector == null || node.Inspector != Inspector)
            throw new ArgumentException($"'{node.Path}' is not a node of the inspector this view shows.", nameof(node));

        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.IsGroup)
                parent.Collapsed = false;
        }

        PendingFocus = node;
    }

    public void DrawWindow()
    {
        try
        {
            if (Gui.Begin(Title))
                Draw();
        }
        finally
        {
            Gui.End();
        }
    }

    // Draws into the current window, for hosts that place the view themselves.
    public void Draw()
    {
        if (Inspector is not { } inspector)
            return;

        if (RefreshEachFrame)
            inspector.Refresh();

        // Taken for this frame only: a node that does not draw in it (hidden, ignored) is not focused later.
        var focus = PendingFocus;
        PendingFocus = null;

        const ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg;

        if (!Gui.BeginTable("##inspector", 2, flags))
            return;

        try
        {
            Gui.TableSetupColumn("Member", ImGuiTableColumnFlags.WidthFixed, 180f);
            Gui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            DrawRows(inspector, focus);
        }
        finally
        {
            Gui.EndTable();
        }
    }

    public void Dispose()
    {
        Detach();
        Release();
    }

    // Values are user data: ImGui's Text and SetTooltip take printf formats, so a '%' in a value would be read as one.
    internal static void Text(string text, Vector4? color = null)
    {
        if (color is { } c)
            Gui.PushStyleColor(ImGuiCol.Text, c);

        Gui.PushTextWrapPos(0f);
        Gui.TextUnformatted(text);
        Gui.PopTextWrapPos();

        if (color != null)
            Gui.PopStyleColor();
    }

    // A tooltip of user text, wrapped, for the item drawn last; also over a disabled editor. The detail
    // goes grey under the text, when there is one.
    internal static void Tooltip(string? text, string? detail = null)
    {
        var hasText = !string.IsNullOrEmpty(text);

        if (!hasText && string.IsNullOrEmpty(detail))
            return;

        if (!Gui.IsItemHovered(ImGuiHoveredFlags.ForTooltip | ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        Gui.BeginTooltip();
        Gui.PushTextWrapPos(Gui.GetFontSize() * 35f);

        if (hasText)
            Gui.TextUnformatted(text);

        if (!string.IsNullOrEmpty(detail))
        {
            if (hasText)
                Gui.PushStyleColor(ImGuiCol.Text, Gui.GetColorU32(ImGuiCol.TextDisabled));

            Gui.TextUnformatted(detail);

            if (hasText)
                Gui.PopStyleColor();
        }

        Gui.PopTextWrapPos();
        Gui.EndTooltip();
    }

    // The rows come in the order a view shows them, a group followed by the rows inside it, so the tree is
    // the chain of parents: an open group pushes a level for its rows, a row that is not inside it closes
    // the level, and the rows inside a collapsed group are passed over. The rows are taken before any is
    // drawn, so a write from a row (a button that adds a node) changes the next frame, not this one.
    private void DrawRows(Inspector inspector, InspectorNode? focus)
    {
        var levels = new Stack<InspectorNode>();
        InspectorNode? collapsed = null;

        try
        {
            foreach (var node in inspector.Rows.ToList())
            {
                if (collapsed != null && IsInside(node, collapsed))
                    continue;

                collapsed = null;

                while (levels.Count > 0 && levels.Peek() != node.Parent)
                {
                    levels.Pop();
                    Gui.TreePop();
                }

                if (!RowsByNode.TryGetValue(node, out var row) || !row.Fits())
                {
                    row = new ImGuiRow(node);
                    RowsByNode[node] = row;
                }

                if (row.Draw(node == focus))
                {
                    Gui.TreePush(node.Name);
                    levels.Push(node);
                }
                else if (node.IsGroup)
                {
                    collapsed = node;
                }
            }
        }
        finally
        {
            while (levels.Count > 0)
            {
                levels.Pop();
                Gui.TreePop();
            }
        }
    }

    private static bool IsInside(InspectorNode node, InspectorNode group)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent == group)
                return true;
        }

        return false;
    }

    // Lets the inspector go: no more events from it, and no rows.
    private void Release()
    {
        if (Inspector == null)
            return;

        Inspector.Disposed -= OnInspectorDisposed;
        Inspector = null;
        RowsByNode.Clear();
        PendingFocus = null;
    }

    private void OnInspectorDisposed(object? sender, InspectorEventArgs e) => Release();
}
