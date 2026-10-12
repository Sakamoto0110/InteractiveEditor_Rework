# ImGui View

**Document ID:** VIEW-IMGUI-INTRODUCTION

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** introduction to the view-imgui module

**Surface:** introduction

**Boundary:** view-imgui

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

The Dear ImGui view (`views/PixieInspector.ImGui/`) draws an inspector as a
two-column table, every frame, inside the host's ImGui frame. It is the view
PixieEngine's editor integrates: the host owns the window, the loop, and the
ImGui context, and calls the view between `NewFrame` and `Render`.

```csharp
var view = inspector.CreateImGuiView();
view.Title = "Inspector###inspector";
// every frame, between ImGui.NewFrame() and ImGui.Render():
view.DrawWindow();
```

Its most important boundary: the view and its host must use the same ImGui
binding and version, today Hexa.NET.ImGui 2.2.9, because a binding brings its
own native library and context.

The [technical reference](REFERENCE.md) holds the contract, and the
[manifest](MANIFEST.md) routes the rest.
