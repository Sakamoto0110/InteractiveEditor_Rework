# Core Module Manifest

**Document ID:** CORE-MANIFEST

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** identity and routing of the core module

**Surface:** manifest

**Boundary:** core

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

```text
Projects: PixieInspector/PixieInspector.csproj
Language: csharp
Artifact: managed-assembly
Platforms: windows, linux, macos
Target frameworks: net10.0
Module dependencies: none
Third-party dependencies: PixieLib
Distribution: unshipped
Stability: preview
Public API: PixieInspector/PixieInspector.csproj
Profiles: none
Technical reference: docs/modules/core/REFERENCE.md
Related boundaries: view-imgui, view-avalonia, view-winforms, view-wpf
Source: PixieInspector
Tests: none
Runtime scenarios: hosts/TuxHost, hosts/TerminalHost
```

## Routes

- [Introduction](README.md) and [technical reference](REFERENCE.md).
- The decisions behind the core, numbered by question, are in
  [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md); the
  source cites them as `P1.12` or as a section number such as `3.11`.
- Proposals that touch the core:
  [PXI-PROPOSAL-002](../../proposals/vector-editor.md),
  [PXI-PROPOSAL-004](../../proposals/manual-add-without-warning.md),
  [PXI-PROPOSAL-007](../../proposals/flags-enum-editor.md), and
  [PXI-PROPOSAL-008](../../proposals/previous-value-in-value-changed.md).
- The four views build against the core and see its internal `ViewRules`
  through `InternalsVisibleTo`.
- No test project exists. The TuxHost output and the TerminalHost `--dump`
  check are the versioned checks; see the
  [development guide](../../development.md#checks).
- No on-demand surface exists yet.
