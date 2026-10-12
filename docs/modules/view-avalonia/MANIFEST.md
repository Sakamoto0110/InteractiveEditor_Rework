# Avalonia View Module Manifest

**Document ID:** VIEW-AVALONIA-MANIFEST

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** identity and routing of the view-avalonia module

**Surface:** manifest

**Boundary:** view-avalonia

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

```text
Projects: views/PixieInspector.Avalonia/PixieInspector.Avalonia.csproj
Language: csharp
Artifact: managed-assembly
Platforms: windows, linux, macos
Target frameworks: net10.0
Module dependencies: core
Third-party dependencies: Avalonia
Distribution: unshipped
Stability: preview
Public API: views/PixieInspector.Avalonia/PixieInspector.Avalonia.csproj
Profiles: none
Technical reference: docs/modules/view-avalonia/REFERENCE.md
Related boundaries: core, view-imgui, view-winforms, view-wpf
Source: views/PixieInspector.Avalonia
Tests: none
Runtime scenarios: hosts/AvaloniaHost
```

## Routes

- [Introduction](README.md) and [technical reference](REFERENCE.md).
- The core it builds against: [`core`](../core/REFERENCE.md), including the
  internal `ViewRules` it shares with the other views.
- The sample that shows it: [`hosts/AvaloniaHost`](../../../hosts/AvaloniaHost/README.md).
- The decisions behind the views (P7.x) are in
  [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md), section 0
  (Views) and section 3.5.
- No proposal touches this view yet.
- No test project exists; no on-demand surface exists yet.
