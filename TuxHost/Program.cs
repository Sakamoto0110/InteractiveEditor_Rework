using DemoObjects.AttributedObjects;
using DemoObjects.ClassObjects;
using InteractiveEditor;
using InteractiveEditor.Options;

namespace TuxHost;

internal class Program
{
    static void Main(string[] args)
    {
        Print("Foo: reflection only", Inspector.Create<Foo>(), new Foo());

        var foo = Inspector.Create<Foo>();
        foo["x"].Label = "Renamed X";
        foo["x"].ScrubMultiplier = 1;
        foo["Moo.MooX"].Tooltip = "Moo X position";
        foo["Moo"].Collapsed = true;
        foo["Moo2"].Ignored = true;
        foo["Moo2"]["MooY"].ReadOnly = true;
        Print("Foo: manual layer", foo, new Foo());

        var boo = Inspector.Create<Boo>();
        boo["Id"].ReadOnly = false;
        Print("Boo: attributes, with Id made writable by the manual layer", boo, new Boo());

        GlobalOptions.RequireExpandableAttribute = true;
        Print("Boo: GlobalOptions.RequireExpandableAttribute = true", Inspector.Create<Boo>(), new Boo());
        GlobalOptions.RequireExpandableAttribute = false;

        Console.WriteLine("== Foo: unknown path");

        using var unknown = Inspector.Create<Foo>();

        try
        {
            unknown["Moo.Nope"].Label = "?";
        }
        catch (KeyNotFoundException e)
        {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
        }
    }

    static void Print(string title, Inspector inspector, object instance)
    {
        inspector.Bind(instance);

        Console.WriteLine($"== {title}");

        foreach (var node in inspector.Rows)
        {
            var indent = new string(' ', 2 * node.Path.Count(c => c == '.'));
            var value = node.IsGroup ? "" : $" = {node.GetValue()}";

            Console.WriteLine($"{indent}{node.Label}{value}   [{Describe(node)}]");
        }

        Console.WriteLine();

        // Print owns the inspector: disposing it releases its lock on the global options.
        inspector.Dispose();
    }

    static string Describe(InspectorNode node)
    {
        var parts = new List<string> { node.Path, node.IsGroup ? "group" : node.Editor.ToString() };

        if (node.ReadOnly)
            parts.Add("read-only");

        if (node.Collapsed)
            parts.Add("collapsed");

        if (node.ScrubMultiplier is { } scrub)
            parts.Add($"scrub x{scrub}");

        if (node.Range is { } range)
            parts.Add($"range {range.Min}..{range.Max} step {range.Step}");

        if (node.Tooltip is { } tooltip)
            parts.Add($"tooltip \"{tooltip}\"");

        if (node.Help is { } help)
            parts.Add($"help \"{help}\"");

        return string.Join(", ", parts);
    }
}
