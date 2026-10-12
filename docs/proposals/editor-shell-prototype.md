# Editor Shell Prototype in ImGuiHost

**Document ID:** GLOBAL-PROPOSAL-EDITOR-SHELL

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** a prototype of the PixieEngine editor layout in the ImGui host

**Surface:** proposal

**Boundary:** view-imgui

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-001

**State:** idea

**Idea:** Turn `hosts/ImGuiHost` into a prototype of the editor shell the owner
sketched on 2026-10-11: a menu bar, a dockspace with a scene panel in the
center, an editor panel and another dock on the left, an asset manager and a
debug dock on the right, and a status bar fed by the inspector's reports and
events. The host creates an OpenGL 4.5 core context and enables multi-viewport,
so the scene panel can leave for a window of its own. The scene comes from a
stand-in engine written in C# behind the same contract the native engine offers:
`Update`, `Render`, and a texture. The editor panel stacks one inspector per
component, each created in manual mode.

**Rationale:** PixieEngine plans a C# desktop editor that owns the window and
the loop, gives the native engine an active OpenGL 4.5 core context, calls the
engine's `Update` and `Render` on the UI thread, and shows the scene as a
texture. The editor uses this repository's ImGui view, with docking and
multi-viewport. Engine components reach C# as proxy classes whose properties
call the engine's C API, and the host builds each inspector in manual mode,
since the proxies carry no inspector attributes. A prototype proves the whole
flow (select, edit, see the failure in the status bar, undock the scene) before
the engine exists, and gives the editor a host to copy. The real engine replaces
only the stand-in.

**Constraints:** The host today creates an OpenGL 3.3 core context.
Multi-viewport needs the host to save and restore the current context around
`UpdatePlatformWindows` and `RenderPlatformWindowsDefault`, so the engine always
draws in its own context. Stacking inspectors in one window depends on
PXI-PROPOSAL-005. The stand-in must not grow into an engine.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
