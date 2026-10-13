# ImGuiHost Fault at Exit

**Document ID:** ARCHIVE-IMGUI-HOST-EXIT-FAULT-20261013

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the access violation the ImGui host raised after `Main` returned, its cause in the Hexa.NET backends, and the shutdown order that avoids it

**Surface:** archive-record

**Boundary:** view-imgui

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-013

**Type:** fix

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-13

**Commits:** `2abd0f8c8692ee71d8af0393a2e2b70ead250954`

**Current state:** [`docs/modules/view-imgui/REFERENCE.md`](../modules/view-imgui/REFERENCE.md#binding-and-host)

PixieEngine's editor copies the ImGuiHost's GLFW and ImGui setup, and it
faulted at exit. The owner had that session pass the report here: the
ImGuiHost exited with `0xC0000005` in 4 of 6 runs, closed with
`CloseMainWindow` after 2 seconds, on Windows 11 with an AMD Radeon RX 7600.
Every shutdown step completed; the fault came after `Main` returned, also
with no engine loaded.

## Cause

A scratch console outside the repository, with the host's packages
(Hexa.NET.GLFW 1.0.5, Hexa.NET.ImGui 2.2.9, the Backends 1.0.18), isolated it:

| Run | Faults |
|---|---|
| GLFW window, ImGui and both backends, the host's shutdown | 8 of 10, then 13 of 15 |
| The same, with `opengl32.dll` loaded first and never freed | 7 of 10 |
| The same, without `GLFW.Terminate` | 10 of 10 |
| GLFW window only, no ImGui | 0 of 10 |

The Application log named the faulting module: `ImGuiImplGLFW.dll`, offset
`0x1f74`. That DLL imports no ImGui library; it compiles in the ImGui
functions it uses and keeps its own current context, set by
`ImGuiImplGLFW.SetCurrentContext`. The instructions at the offset read this
copy and, when it is not null, a 16-bit field at `+0x2aa0` and take it modulo 6:
`ImGui::MemFree` updating the context's debug allocation log
(`LastEntriesIdx`). `ImGui.DestroyContext` frees the context and clears
cimgui's copy only, so the backend's copy points at freed memory. At process
exit the backend frees a static vector of its own through `MemFree`, which
reads the freed context; whether the page is still mapped decides whether the
process faults. An earlier guess, the AMD driver unloading with
`opengl32.dll`, did not hold: the DLL stayed loaded, and pinning it changed
nothing.

## Fix

After `ImGuiImplOpenGL3.Shutdown` and `ImGuiImplGLFW.Shutdown`, the host sets
both backends' context to null (`SetCurrentContext(default)`), then calls
`DestroyContext`. `MemFree` then skips the context and only frees.

## Verification

- Scratch console with the fix: 0 of 15, and 0 of 15 without
  `GLFW.Terminate`; the baseline in the same session: 13 of 15.
- ImGuiHost at `2abd0f8`, closed 12 times with `CloseMainWindow` after
  2 seconds: 0 faults.

Only this machine and driver were checked. The cause is in the backend DLL,
not the driver, so the fix does not depend on them.
