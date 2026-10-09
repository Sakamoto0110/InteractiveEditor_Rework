using System.Numerics;
using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.StructObjects;
using DemoObjects.ViewObjects;
using ImGuiNET;
using InteractiveEditor;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.ImGui;
using Silk.NET.Core.Loader;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
// Two of the demo types are called Boo: a class with attributes, and a struct.
using AttributedBoo = DemoObjects.AttributedObjects.Boo;
using StructBoo = DemoObjects.StructObjects.Boo;

namespace ImGuiHost;

// The Dear ImGui view of the demo objects on a Silk.NET window: the list of them on the left, with the
// last value written or the last failure under it, and the view of the one chosen on the right. Each comes
// with an inspector of its own, created and bound when it is chosen, and disposed when another is.
internal sealed class Program : IImGuiHost
{
    private static readonly (string Name, Func<Inspector> Create)[] DemoTargets =
    [
        ("Gadget", CreateGadget),
        ("Foo", () => Bind(new Foo())),
        ("Moo", () => Bind(new Moo())),
        ("Doo", () => Bind(new Doo())),
        ("Boo (attributes)", () => Bind(new AttributedBoo())),
        ("Boo (struct)", () => Bind(new StructBoo { BooX = 1, BooY = 2, Coo = new Coo { CooX = 3, CooY = 4.5f } })),
        ("Coo (struct)", () => Bind(new Coo { CooX = 3, CooY = 4.5f })),
        ("Hoo", () => Bind(new Hoo())),
    ];

    private Inspector? Current;
    private ImGuiInspectorView? View;
    private int Selected = -1;
    private string Status = string.Empty;

    // The frame of the last value written into the status, so a write shows its own node and not the
    // struct or the collection that changed along with it.
    private int StatusFrame = -1;

    public event Action? Frame;

    [STAThread]
    private static void Main()
    {
        AddRuntimeNativeDirectories();
        new Program().Run();
    }

    // Silk.NET 2.x looks for its native libraries (GLFW) without probing runtimes/<rid>/native, so on Linux it
    // fails to find the one NuGet ships. The runtime already resolved those folders from deps.json; hand them over.
    private static void AddRuntimeNativeDirectories()
    {
        var directories = (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string)?
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];

        if (PathResolver.Default is DefaultPathResolver resolver)
            resolver.Resolvers.Add(name => directories.Select(directory => Path.Combine(directory, name)));
    }

    private void Run()
    {
        var options = WindowOptions.Default with
        {
            Title = "InteractiveEditor - ImGui host",
            Size = new Vector2D<int>(1000, 720)
        };

        using var window = Window.Create(options);

        GL? gl = null;
        IInputContext? input = null;
        ImGuiController? controller = null;

        window.Load += () =>
        {
            gl = window.CreateOpenGL();
            input = window.CreateInput();
            controller = new ImGuiController(gl, window, input);
            LoadTarget(0);
        };

        window.FramebufferResize += size => gl?.Viewport(size);

        window.Render += delta =>
        {
            controller!.Update((float)delta);

            gl!.ClearColor(0.11f, 0.11f, 0.13f, 1f);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            DrawTargets();

            // Applies to the attached view's window, which Begins next.
            ImGui.SetNextWindowPos(new Vector2(220, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(770, 700), ImGuiCond.FirstUseEver);
            Frame?.Invoke();

            controller.Render();
        };

        window.Closing += () =>
        {
            View?.Dispose();
            Current?.Dispose();
            controller?.Dispose();
            input?.Dispose();
            gl?.Dispose();
        };

        window.Run();
    }

    private void DrawTargets()
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(200, 700), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Targets"))
        {
            for (var i = 0; i < DemoTargets.Length; i++)
            {
                if (ImGui.Selectable(DemoTargets[i].Name, Selected == i))
                    LoadTarget(i);
            }

            ImGui.Separator();
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(Status);
            ImGui.PopTextWrapPos();
        }

        ImGui.End();
    }

    private void LoadTarget(int index)
    {
        var (name, create) = DemoTargets[index];

        // The last view lets its inspector go, and the inspector its objects and the global options.
        View?.Dispose();
        Current?.Dispose();

        Current = create();
        Watch(Current);

        // "###inspector" keeps one window (position, size) across targets while the label changes.
        View = Current.CreateImGuiView();
        View.Title = $"{name}###inspector";
        View.AttachToHost(this);

        Selected = index;
        Status = $"Loaded {name}";
    }

    // The status follows the core's events: a value the view wrote, and what failed.
    private void Watch(Inspector inspector)
    {
        foreach (var node in inspector)
        {
            node.ValueChanged += OnValueChanged;
            node.BindFailed += OnBindFailed;
        }
    }

    private void OnValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (e.Source != ValueSource.Write || ImGui.GetFrameCount() == StatusFrame)
            return;

        StatusFrame = ImGui.GetFrameCount();
        var value = e.Node is CollectionNode collection ? $"{collection.Items.Count} items" : e.Node.ViewValue ?? "null";
        Status = $"{e.Node.Path} = {value}";
    }

    private void OnBindFailed(object? sender, InspectorFailureEventArgs e)
    {
        Status = $"{e.Path}: {e.Reason}";
    }

    private static Inspector Bind<T>(T instance) where T : notnull
    {
        var inspector = Inspector.Create<T>();
        inspector.Bind(instance);
        return inspector;
    }

    // A member for each editor, with a display and a button added by hand, and a button that binds a
    // second gadget, for a look at the values they do not share (P7.19), as in WindowsHost.
    private static Inspector CreateGadget()
    {
        var gadget = new Gadget();
        var second = new Gadget { Name = "second", Count = 7, Ratio = 0.25, Enabled = false, Mode = GadgetMode.Fast };
        var inspector = Inspector.Create<Gadget>();

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

        inspector.Bind(gadget);
        return inspector;
    }
}
