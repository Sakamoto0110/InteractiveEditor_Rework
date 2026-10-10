using System.Numerics;
using DemoObjects.ClassObjects;
using DemoObjects.HybridObjects;
using DemoObjects.StructObjects;
using DemoObjects.ViewObjects;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using InteractiveEditor;
using InteractiveEditor.Diagnostics;
using InteractiveEditor.Events;
using InteractiveEditor.ImGui;
// Hexa.NET.GLFW and the GLFW backend each have a window handle type, over the same GLFW window.
using BackendWindow = Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr;
using GLFWmonitorPtr = Hexa.NET.GLFW.GLFWmonitorPtr;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;
// Two of the demo types are called Boo: a class with attributes, and a struct.
using AttributedBoo = DemoObjects.AttributedObjects.Boo;
using StructBoo = DemoObjects.StructObjects.Boo;

namespace ImGuiHost;

// The Dear ImGui view of the demo objects on a GLFW window, through ImGui's own GLFW and OpenGL3 backends,
// with docking on: the list of them on the left, with the last value written or the last failure under it,
// and the view of the one chosen on the right, both docked in a dockspace over the window. Each comes with
// an inspector of its own, created and bound when it is chosen, and disposed when another is.
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
    private static void Main() => new Program().Run();

    private unsafe void Run()
    {
        if (GLFW.Init() == 0)
            throw new InvalidOperationException("GLFW could not start.");

        // OpenGL 3.3 core, which macOS gives only forward compatible.
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 3);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_FORWARD_COMPAT, GLFW.GLFW_TRUE);

        var window = GLFW.CreateWindow(1000, 720, "InteractiveEditor - ImGui host", default(GLFWmonitorPtr), default(GLFWwindowPtr));

        if (window.Handle == null)
        {
            GLFW.Terminate();
            throw new InvalidOperationException("GLFW could not open an OpenGL 3.3 window.");
        }

        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(1);

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        // The layout of the docked windows goes to imgui.ini, next to where the host runs; with none yet,
        // the first frame docks them side by side.
        var firstLayout = !File.Exists("imgui.ini");
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

        ImGuiImplGLFW.SetCurrentContext(context);
        ImGuiImplGLFW.InitForOpenGL(new BackendWindow((Hexa.NET.ImGui.Backends.GLFW.GLFWwindow*)window.Handle), true);
        ImGuiImplOpenGL3.SetCurrentContext(context);
        ImGuiImplOpenGL3.Init("#version 150");

        LoadTarget(0);

        while (GLFW.WindowShouldClose(window) == 0)
        {
            GLFW.PollEvents();
            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplGLFW.NewFrame();
            ImGui.NewFrame();

            // The dockspace covers the window and paints its background, so the frame needs no clear.
            var dockspace = ImGui.GetID("dockspace");

            if (firstLayout)
            {
                DockSideBySide(dockspace);
                firstLayout = false;
            }

            ImGui.DockSpaceOverViewport(dockspace);

            DrawTargets();

            // Applies to the attached view's window, which Begins next, for when it is not docked.
            ImGui.SetNextWindowPos(new Vector2(220, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(770, 700), ImGuiCond.FirstUseEver);
            Frame?.Invoke();

            ImGui.Render();
            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
            GLFW.SwapBuffers(window);
        }

        View?.Dispose();
        Current?.Dispose();
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplGLFW.Shutdown();
        ImGui.DestroyContext(context);
        GLFW.DestroyWindow(window);
        GLFW.Terminate();
    }

    // The targets on the left fifth of the dockspace, and the inspector's window, by the ID its titles keep
    // ("###inspector"), on the rest.
    private static unsafe void DockSideBySide(uint dockspace)
    {
        ImGuiP.DockBuilderRemoveNode(dockspace);
        ImGuiP.DockBuilderAddNode(dockspace, (ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.Space);
        ImGuiP.DockBuilderSetNodeSize(dockspace, ImGui.GetMainViewport().Size);

        uint left, right;
        ImGuiP.DockBuilderSplitNode(dockspace, ImGuiDir.Left, 0.2f, &left, &right);
        ImGuiP.DockBuilderDockWindow("Targets", left);
        ImGuiP.DockBuilderDockWindow("###inspector", right);
        ImGuiP.DockBuilderFinish(dockspace);
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
