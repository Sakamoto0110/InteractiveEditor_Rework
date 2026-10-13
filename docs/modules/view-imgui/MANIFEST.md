# ImGui View Module Manifest

**Document ID:** VIEW-IMGUI-MANIFEST

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** identity and routing of the view-imgui module

**Surface:** manifest

**Boundary:** view-imgui

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

```text
Projects: views/PixieInspector.ImGui/PixieInspector.ImGui.csproj
Language: csharp
Artifact: managed-assembly
Platforms: windows, linux, macos
Target frameworks: net10.0
Module dependencies: core
Third-party dependencies: Hexa.NET.ImGui
Distribution: package
Stability: preview
Public API: views/PixieInspector.ImGui/PixieInspector.ImGui.csproj
Profiles: none
Technical reference: docs/modules/view-imgui/REFERENCE.md
Related boundaries: core, view-avalonia, view-winforms, view-wpf
Source: views/PixieInspector.ImGui
Tests: none
Runtime scenarios: hosts/ImGuiHost
```

## Routes

- [Introduction](README.md) and [technical reference](REFERENCE.md).
- The core it builds against: [`core`](../core/REFERENCE.md), including the
  internal `ViewRules` it shares with the other views.
- The sample that shows it: [`hosts/ImGuiHost`](../../../hosts/ImGuiHost/README.md).
- The decisions behind the views (P7.x) are in
  [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md), section 0
  (Views) and section 3.5.
- Proposals that touch this view:
  [PXI-PROPOSAL-001](../../proposals/editor-shell-prototype.md),
  [PXI-PROPOSAL-002](../../proposals/vector-editor.md),
  [PXI-PROPOSAL-003](../../proposals/imgui-harness.md),
  [PXI-PROPOSAL-005](../../proposals/imgui-view-id-scope.md),
  [PXI-PROPOSAL-006](../../proposals/reference-drop-extension.md).
- No test project exists; no on-demand surface exists yet.
