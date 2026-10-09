using InteractiveEditor;

namespace TerminalHost;

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
            var inspector = target.Create();

            Console.WriteLine(inspector.Name);
            Print(inspector, 1);

            var results = BindingCheck.Run(inspector).ToList();
            var failed = results.Where(r => !r.Ok).ToList();

            foreach (var result in failed)
                Console.WriteLine($"  MISMATCH {Describe(result)}");

            Console.WriteLine($"  bindings: {results.Count - failed.Count}/{results.Count} ok");
            Console.WriteLine();

            mismatches += failed.Count;
        }

        return mismatches == 0 ? 0 : 1;
    }

    public static string Describe(BindingResult result)
    {
        var path = result.Node.Descriptor?.FullPath ?? result.Node.Name;

        return result.Error != null
            ? $"{path}: {result.Error}"
            : $"{path}: inspector {NodeInfo.Format(result.Bound)}, object {NodeInfo.Format(result.Direct)}";
    }

    private static void Print(Inspector inspector, int depth)
    {
        foreach (var node in inspector.Nodes)
        {
            Console.WriteLine($"{new string(' ', depth * 2)}{NodeInfo.Label(node)}");

            if (node is Inspector nested)
                Print(nested, depth + 1);
        }
    }
}
