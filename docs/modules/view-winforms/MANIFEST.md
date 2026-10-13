# WinForms View Module Manifest

**Document ID:** VIEW-WINFORMS-MANIFEST

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** identity and routing of the view-winforms module

**Surface:** manifest

**Boundary:** view-winforms

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

```text
Projects: views/PixieInspector.WinForms/PixieInspector.WinForms.csproj
Language: csharp
Artifact: managed-assembly
Platforms: windows
Target frameworks: net10.0-windows
Module dependencies: core
Third-party dependencies: none
Distribution: package
Stability: preview
Public API: views/PixieInspector.WinForms/PixieInspector.WinForms.csproj
Profiles: none
Technical reference: docs/modules/view-winforms/REFERENCE.md
Related boundaries: core, view-imgui, view-avalonia, view-wpf
Source: views/PixieInspector.WinForms
Tests: none
Runtime scenarios: hosts/WindowsHost
```

## Routes

- [Introduction](README.md) and [technical reference](REFERENCE.md).
- The core it builds against: [`core`](../core/REFERENCE.md), including the
  internal `ViewRules` it shares with the other views.
- The sample that shows it: [`hosts/WindowsHost`](../../../hosts/WindowsHost/README.md).
- The decisions behind the views (P7.x) are in
  [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md), section 0
  (Views) and section 3.5.
- No proposal touches this view yet.
- No test project exists; no on-demand surface exists yet.
