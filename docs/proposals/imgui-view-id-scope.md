# An ImGui ID Scope per View

**Document ID:** GLOBAL-PROPOSAL-IMGUI-ID-SCOPE

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** letting several ImGui inspector views share one window

**Surface:** proposal

**Boundary:** view-imgui

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-005

**State:** idea

**Idea:** `ImGuiInspectorView.Draw` pushes an ID scope of its own (for example
the inspector's `Id`) around its table, so several views can draw in the same
ImGui window.

**Rationale:** The view's table has a fixed ID (`##inspector`), and the rows are
identified by node name. Two views drawn in one window collide. The PixieEngine
editor stacks one inspector per component in its editor panel. A host can push
the ID itself today, but every host would have to know to do it.

**Constraints:** None found in the view; `DrawWindow` already gives each view a
window of its own.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
