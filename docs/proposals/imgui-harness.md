# ImGui Verification Harness in the Repository

**Document ID:** GLOBAL-PROPOSAL-IMGUI-HARNESS

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** bringing the headless ImGui verification harness into the repository

**Surface:** proposal

**Boundary:** view-imgui

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-003

**State:** idea

**Idea:** Add the headless harness that drives the ImGui view through ImGui's
own input queue, with no window or GPU, as a console project under the
repository: 25 scenarios covering text, numbers, toggle, choice, slider, color,
selector, list, button, mixed values, struct write-back, nullables, refresh, and
dispose. It runs from the command line and prints one line per scenario.

**Rationale:** The harness validated the port from ImGui.NET to Hexa.NET.ImGui
2.2.9, and caught a real regression (Esc committing typed text). It lives
outside the repository today and is lost with the session that built it. The
owner does not want to be tied to one ImGui binding; the harness is what makes a
later port cheap and safe.

**Constraints:** Test consoles have so far lived outside the repository. Under
the documentation policy, a project with a purpose beyond demonstration
documents its claim, expected outcome, and how to read the result. Headless runs
under ImGui 1.92 must accept the renderer's textures (`RendererHasTextures`) and
keep the navigation cursor visible.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
