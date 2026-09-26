using DemoObjects.AttributedObjects;
using DemoObjects.ClassObjects;
using InteractiveEditor;
using InteractiveEditor.Binding;
using InteractiveEditor.Options;

namespace NoHost;

internal class Program
{
    static void Main(string[] args)
    {
        Print("Foo: reflection only", Inspector.Create<Foo>(), new Foo());

        Print("Foo: manual layer", Inspector.Create<Foo>(fields =>
        {
            fields["x"].Label = "Renamed X";
            fields["x"].ScrubMultiplier = 1;
            fields["Moo.MooX"].Tooltip = "Moo X position";
            fields["Moo"].Collapsed = true;
            fields["Moo2"].Ignored = true;
        }), new Foo());

        Print("Boo: attributes, with Id made writable by the manual layer", Inspector.Create<Boo>(fields =>
        {
            fields["Id"].ReadOnly = false;
        }), new Boo());

        GlobalOptions.RequireExpandableAttribute = true;
        Print("Boo: GlobalOptions.RequireExpandableAttribute = true", Inspector.Create<Boo>(), new Boo());
        GlobalOptions.RequireExpandableAttribute = false;

        Console.WriteLine("== Foo: unknown path");

        try
        {
            Inspector.Create<Foo>(fields => fields["Moo.Nope"].Label = "?");
        }
        catch (KeyNotFoundException e)
        {
            Console.WriteLine($"{e.GetType().Name}: {e.Message}");
        }
    }

    static void Print(string title, Inspector inspector, object instance)
    {
        inspector.bind(instance);

        Console.WriteLine($"== {title}");

        foreach (var node in inspector)
        {
            var options = node.Options!;
            var indent = new string(' ', 2 * options.Path.Count(c => c == '.'));
            var value = node is Fieldset ? $" = {node.GetValue()}" : "";

            Console.WriteLine($"{indent}{options.Label}{value}   [{Describe(options)}]");
        }

        Console.WriteLine();
    }

    static string Describe(FieldOptions options)
    {
        var parts = new List<string> { options.Path, options.IsGroup ? "group" : options.Editor.ToString() };

        if (options.ReadOnly)
            parts.Add("read-only");

        if (options.Collapsed)
            parts.Add("collapsed");

        if (options.ScrubMultiplier is { } scrub)
            parts.Add($"scrub x{scrub}");

        if (options.Range is { } range)
            parts.Add($"range {range.Min}..{range.Max} step {range.Step}");

        if (options.Tooltip is { } tooltip)
            parts.Add($"tooltip \"{tooltip}\"");

        if (options.Help is { } help)
            parts.Add($"help \"{help}\"");

        return string.Join(", ", parts);
    }
}
