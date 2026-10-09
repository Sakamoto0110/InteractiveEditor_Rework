using InteractiveEditor;

namespace TerminalHost;

// The non-interactive mode: the whole tree of each target, as discovered (ignored and hidden nodes too,
// marked), what went wrong in its Create, and the binding check. The exit code is 1 when a binding does
// not match, so it can run in a script.
internal static class Dump
{
    public static int Run(string? targetName)
    {
        IReadOnlyList<DemoTarget> targets = DemoTargets.All;

        if (targetName != null)
        {
            if (DemoTargets.Find(targetName) is not { } target)
            {
                Console.Error.WriteLine($"Unknown target '{targetName}'. Available: {string.Join(", ", DemoTargets.All.Select(t => t.Name))}");
                return 2;
            }

            targets = [target];
        }

        var mismatches = 0;

        foreach (var target in targets)
        {
            // The dump owns the inspector: disposing it releases its hold on the global options.
            using var inspector = target.Create();

            Console.WriteLine(target.Name == inspector.Name ? target.Name : $"{target.Name} ({inspector.Name})");

            foreach (var failure in inspector.Report.Failures)
                Console.WriteLine($"  create: {failure.Path}: {failure.Message} {failure.Reason}");

            Print(inspector.Where(node => node.Parent is { Parent: null }), inspector.Rows.ToHashSet(), 1);

            var results = BindingCheck.Run(inspector).ToList();
            var failed = results.Where(r => !r.Ok).ToList();

            foreach (var result in failed)
                Console.WriteLine($"  MISMATCH {result.Describe()}");

            Console.WriteLine($"  bindings: {results.Count - failed.Count}/{results.Count} ok");
            Console.WriteLine();

            mismatches += failed.Count;
        }

        return mismatches == 0 ? 0 : 1;
    }

    // Siblings in their Order, as a view puts them; a node the view does not show as a row (ignored,
    // hidden, or below a member that is not a group) says so.
    private static void Print(IEnumerable<InspectorNode> nodes, HashSet<InspectorNode> rows, int depth)
    {
        foreach (var node in nodes.OrderBy(node => node.Order))
        {
            var row = rows.Contains(node) ? string.Empty : ", not a row";

            Console.WriteLine($"{new string(' ', depth * 2)}{NodeInfo.Label(node)}   [{NodeInfo.Tags(node)}{row}]");
            Print(node.Where(child => child.Parent == node), rows, depth + 1);
        }
    }
}
