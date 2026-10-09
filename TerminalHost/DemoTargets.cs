using System.Globalization;
using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.StructObjects;
using DemoObjects.ViewObjects;
using InteractiveEditor;
using AttributedBoo = DemoObjects.AttributedObjects.Boo;

namespace TerminalHost;

// A demo object and how its inspector is made. Each Create makes new objects, so F7 starts over.
internal sealed record DemoTarget(string Name, Func<Inspector> Create);

internal static class DemoTargets
{
    public static readonly IReadOnlyList<DemoTarget> All =
    [
        new("Foo", () => Bind(new Foo())),
        new("Moo", () => Bind(new Moo())),
        new("Doo", () => Bind(new Doo())),
        new("Hoo", () => Bind(new Hoo())),
        new("Boo", () => Bind(new Boo { BooX = 1, BooY = 2, Coo = new Coo { CooX = 3, CooY = 4.5f } })),
        new("AttributedBoo", () => Bind(new AttributedBoo())),
        new("Gadget", CreateGadget),
    ];

    public static DemoTarget? Find(string name)
    {
        return All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    // The host reads and shows text in the invariant culture, as it always did, so the dump is the same
    // on every machine and a number shown can be typed back.
    private static Inspector Bind<T>(T instance) where T : notnull
    {
        var inspector = Inspector.Create<T>();
        inspector.Options.Culture = CultureInfo.InvariantCulture;
        inspector.Bind(instance);
        return inspector;
    }

    // A member for each editor, with a button and a display added by hand, and a button that binds a
    // second gadget, for the values the two do not share (P7.19); as WindowsHost shows it.
    private static Inspector CreateGadget()
    {
        var gadget = new Gadget();
        var second = new Gadget { Name = "second", Count = 7, Ratio = 0.25, Enabled = false, Mode = GadgetMode.Fast };
        var inspector = Bind(gadget);

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

        return inspector;
    }
}
