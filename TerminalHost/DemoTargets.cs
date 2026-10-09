using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.StructObjects;
using InteractiveEditor;

namespace TerminalHost;

internal record DemoTarget(string Name, Func<Inspector> Create);

internal static class DemoTargets
{
    public static readonly IReadOnlyList<DemoTarget> All =
    [
        new("Foo", () => Bind(new Foo())),
        new("Moo", () => Bind(new Moo())),
        new("Hoo", () => Bind(new Hoo())),
        new("Boo", () => Bind(new Boo { BooX = 1, BooY = 2, Coo = new Coo { CooX = 3, CooY = 4.5f } })),
    ];

    public static DemoTarget? Find(string name) =>
        All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    private static Inspector Bind<T>(T instance)
    {
        var inspector = Inspector.Create<T>();
        inspector.bind(instance);
        return inspector;
    }
}
