# ImGuiHost

The Dear ImGui view of each demo object, on a GLFW window drawn by ImGui's own
GLFW and OpenGL3 backends (Hexa.NET.ImGui 2.2.9). Docking is on: a dockspace
covers the window, with the list of demo objects on the left, the last value
written or the last failure under it, and the inspector of the chosen object on
the right. Each object gets an inspector of its own, created and bound when it
is chosen and disposed when another is. The `Gadget` gets a `Total` display
and the button that binds a second gadget; it needs no button to read the
object again, because the view reads every frame.

The host creates an OpenGL 3.3 core context. The layout of the docked windows is
saved to `imgui.ini` in the directory the host runs from; with no such file, the
list takes the left fifth and the inspector the rest.

## Build and run

```bash
dotnet run --project hosts/ImGuiHost/ImGuiHost.csproj
```

It runs on Windows and Linux. Without a display, start a virtual one first
(`Xvfb :99 &`, then `DISPLAY=:99`); Mesa's software renderer provides OpenGL.
