# Documentation Index

**Document ID:** GLOBAL-DOCUMENTATION-INDEX

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** entry point to PixieInspector documentation

**Surface:** index

**Boundary:** global

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

Start with the [documentation policy](governance/documentation-policy.md). It
owns the authority table: which document owns which kind of fact. To change
the code, read the [development guide](development.md).

| Location | Contents |
|---|---|
| [`../ROADMAP.md`](../ROADMAP.md) | What PixieInspector is and is not, direction, guardrails, freezes |
| [`../TODO.md`](../TODO.md) | Formally promoted work |
| [`../TASKLIST.md`](../TASKLIST.md) | Task breakdowns of promoted work |
| [`../DEPENDENCIES.md`](../DEPENDENCIES.md) | Dependencies, versions, status, and candidates |
| [`development.md`](development.md) | Building, checking, and committing a change |
| [`modules/`](#modules) | One directory per boundary: the core and each view |
| [`governance/`](governance/documentation-policy.md) | Documentation rules |
| [`schemas/`](schemas/README.md) | Documentation structure and vocabulary, and its derivation |
| [`templates/`](templates/) | Templates for modules, schedulers, proposals, and archive records |
| [`proposals/`](proposals/README.md) | Unpromoted ideas |
| [`archive/`](archive/README.md) | Chronology, completed work, dated rationale, and the documents written before the policy |

## Modules

| Boundary | Project | Introduction | Reference |
|---|---|---|---|
| `core` | `PixieInspector/` | [`README.md`](modules/core/README.md) | [`REFERENCE.md`](modules/core/REFERENCE.md) |
| `view-imgui` | `views/PixieInspector.ImGui/` | [`README.md`](modules/view-imgui/README.md) | [`REFERENCE.md`](modules/view-imgui/REFERENCE.md) |
| `view-avalonia` | `views/PixieInspector.Avalonia/` | [`README.md`](modules/view-avalonia/README.md) | [`REFERENCE.md`](modules/view-avalonia/REFERENCE.md) |
| `view-winforms` | `views/PixieInspector.WinForms/` | [`README.md`](modules/view-winforms/README.md) | [`REFERENCE.md`](modules/view-winforms/REFERENCE.md) |
| `view-wpf` | `views/PixieInspector.Wpf/` | [`README.md`](modules/view-wpf/README.md) | [`REFERENCE.md`](modules/view-wpf/REFERENCE.md) |

The hosts under `hosts/` are samples, documented by a `README.md` in each
directory.
