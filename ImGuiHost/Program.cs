using System.Numerics;
using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.StructObjects;
using ImGuiNET;
using InteractiveEditor;
using InteractiveEditor.ImGui;
using Silk.NET.Core.Loader;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ImGuiHost;

internal sealed class Program : IImGuiHost
{
    private static readonly (string Name, Func<Inspector> Create)[] DemoTargets =
    [
        ("Foo", () => Bind(new Foo())),
        ("Moo", () => Bind(new Moo())),
        ("Hoo", () => Bind(new Hoo())),
        ("Boo", () => Bind(new Boo { BooX = 1, BooY = 2, Coo = new Coo { CooX = 3, CooY = 4.5f } })),
    ];

    private InspectorView? View;
    private int Selected = -1;
    private string Status = string.Empty;

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

        View?.Detach();

        // "###inspector" keeps one window (position, size) across targets while the label changes.
        View = new InspectorView(create(), this) { Title = $"{name}###inspector" };
        View.FieldEdited += (_, e) =>
        {
            var path = e.Field.Descriptor?.FullPath ?? e.Field.Name;

            Status = e.Error == null
                ? $"{path} = {e.Field.GetValue() ?? "null"}"
                : $"{path}: {e.Error.Message}";
        };

        Selected = index;
        Status = $"Loaded {name}";
    }

    private static Inspector Bind<T>(T instance)
    {
        var inspector = Inspector.Create<T>();
        inspector.bind(instance);
        return inspector;
    }
}
