# The PixieEngine Editor Discussion

**Document ID:** ARCHIVE-EDITOR-DISCUSSION-20261012

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** what PixieEngine's editor needs from PixieInspector, as discussed with the owner, and the eight proposals it produced

**Surface:** archive-record

**Boundary:** global

**Authority role:** non-normative

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-008

**Type:** proposal

**Origin:** owner discussion

**Author:** claude

**Reference date:** 2026-10-12

**Commits:** with this record (parent `997c70d68102a1c2a29f080a577f99f4c88054e3`)

**Reference commit:** `2f7e72aa74dc77fcf48112648555dc2630a6bf1d`, the code discussed

**Current state:** [`docs/proposals/`](../proposals/README.md)

On 2026-10-11 and 2026-10-12 the owner asked whether the views were ready for
PixieEngine and sketched a first editor window: a menu bar; on the left a dock
and an editor panel; in the center a scene preview that could leave for a free
window; on the right an asset manager and a debug dock; and a status bar for
reports and errors. The owner relayed the questions to the PixieEngine side and
brought back its answers.

## What PixieEngine answered

- **The host.** The editor is a C# desktop application that owns the window and
  the loop and shows the engine's viewport as a texture. The engine is native
  C++ (C++20, CMake), runs in the same process behind a C API, and never draws
  the editor's UI: the C# inspector could not draw in a C++ ImGui context, and
  the engine never depends on the editor.
- **The contract.** The host provides an active OpenGL 4.5 core context, calls
  the engine's `Update` and `Render`, and receives a texture to show. Any host
  that does this works, whatever its toolkit. macOS is not a target.
- **The view.** The ImGui view, because docking gives the editor's layout and
  multi-viewport lets the viewport leave for a window of its own.
- **The binding.** The engine uses no ImGui binding, so the requirement of one
  binding and one version is entirely on the C# side, between the inspector's
  adapter and the editor's host. Hexa.NET.ImGui 2.2.9 is the binding today
  because the adapter uses it; the owner does not want to be tied to it.
- **The thread.** The game logic runs on one thread, the owner of the GL
  context; `Update` (fixed step) and `Render` share the loop. In the editor,
  the host calls them inside its own frame on the UI thread, so the inspector
  reads and writes on the thread the simulation runs on.
- **The components.** Storage is undecided. C# sees only proxy classes whose
  properties call the C API (PixieEngine's PIXIE-PROPOSAL-002); the native scene
  is the only source of truth, and the inspector reads live values through the
  parent chain.
- **The types that need an editor of their own.** Vectors, points, and sizes
  (the PixieLib `Px` types); colors; the transform, with rotation in degrees;
  entity references and asset references, each with a selector and dragging;
  and enums. PixieLib's unsuffixed point is `int` in C++ and `double` in C#,
  which the bridge has to resolve.

## What was established on this side

- **Where the proxies live.** On the engine side, with no inspector attributes.
  The host builds each inspector in manual mode, which exists for this case:
  types that carry no attributes, where the host creates the members itself.
  Owner decision, 2026-10-12.
- **A proxy must return the same child proxy on every read.** If a property
  returns a new proxy object each time, the core sees the group's object
  replaced on every refresh, raises `ObjectReplaced`, and disables the branch
  until a rebind; this is the outside-replacement detection of P3.3 and P3.4.
  A child proxy should be cached per entity, or be a struct.
- **Checked against the code on 2026-10-12:** a proxy without attributes works
  in manual mode, including a struct member written back through the proxy's
  setter; adding a color by hand raises the P5.8 warning before the host can
  choose its editor; no view and not the core look at `FlagsAttribute`;
  `ValueChangedEventArgs` carries the node and the source but no previous
  value; and the ImGui view's table has a fixed ID, so two views in one window
  collide.

## Outcome

Eight proposals, none promoted:
[PXI-PROPOSAL-001](../proposals/editor-shell-prototype.md) to
[PXI-PROPOSAL-008](../proposals/previous-value-in-value-changed.md). The owner
paused this work to rename the repository and adopt the documentation policy
the same day ([PXI-ARCHIVE-005](rename-pixieinspector-2026-10-12.md) and
[PXI-ARCHIVE-007](documentation-foundation-2026-10-12.md)).
