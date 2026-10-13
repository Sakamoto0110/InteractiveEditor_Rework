# PixieInspector Roadmap

**Document ID:** GLOBAL-ROADMAP

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** what PixieInspector is and is not, direction, guardrails, and freezes

**Surface:** roadmap

**Boundary:** global

**Authority role:** roadmap

**Mutation:** authored

**Indexing:** include

**Direction decision date:** 2026-10-13

## Purpose

This document owns what PixieInspector is, what it is not, where it is heading,
and which boundaries must be preserved. It does not authorize implementation;
[`TODO.md`](TODO.md) is the only scheduler. Unpromoted ideas live in
[`docs/proposals/`](docs/proposals/README.md), and the rationale behind the
decisions below is in the [archive](docs/archive/README.md): the decisions
numbered `P` and the sections cited by the source are in
[PXI-ARCHIVE-001](docs/archive/modernization-notes-2026-10-10.md).

## What PixieInspector is

- A **reflection-based inspector** for .NET: it builds a tree of a type's
  public fields and properties, binds one or more objects to it, and reads and
  writes their members through the parent chain, writing structs back to their
  owners.
- **Options in layers**: what reflection says, then the `[Inspector*]`
  attributes, then the manual layer set in code, which has the last word.
- A **UI-agnostic core** with **one view per framework**: Dear ImGui (through
  Hexa.NET.ImGui), Avalonia, WinForms, and WPF.
- The **inspector of PixieEngine's C# editor**, which builds inspectors in
  manual mode over proxy classes that carry no attributes.
- A **consumer of PixieLib**, which supplies the primitives (points, sizes,
  rectangles, colors).

## What PixieInspector is not

- Not compatible with the 0.7.1a original (`Sakamoto0110/InteractiveEditor`),
  and not a port of it: the original's real use (OverlayApplication) is a
  reference only, and that application is to be rewritten.
- Not for .NET Framework: the core and the views target .NET 10.
- Not an owner of primitive or math types; those belong to PixieLib.
- Not a service locator: finding and applying are the enumeration and the
  indexer.
- Not paged: the views scroll, and an application that wants pages builds
  them on top.

## Current direction

1. Serve PixieEngine's editor. What it needs is recorded in
   [PXI-ARCHIVE-008](docs/archive/editor-discussion-2026-10-12.md) and split
   into eight proposals, none promoted:
   [PXI-PROPOSAL-001](docs/proposals/editor-shell-prototype.md) to
   [PXI-PROPOSAL-008](docs/proposals/previous-value-in-value-changed.md).
2. Keep the views on par: a capability one view gains is a decision for each
   of the others, not an automatic port.

## Active guardrails

- **The inspector does not crash on an internal error.** Discovery,
  reflection, binding, the bound objects, and the views are known weak points;
  a failure in one of them falls back and is reported, and only a fatal one
  reaches the caller. Failures are `Recovered`, `WorkedAround`, `Critical`
  (the failed node is left out), or `Fatal`. Wrong use of the API by the caller
  throws at once, without changing the inspector's state. Accepted 2026-09-27
  and 2026-09-29 (P0.1, P0.2; section 3.11 of PXI-ARCHIVE-001).
- **The options have three layers, applied in order:** reflection, attributes,
  manual. The global options freeze while an inspector is alive (P1.5, P4.4).
- **One project per framework.** The core targets `net10.0` and references no
  UI framework; each view is a project beside it. Accepted 2026-10-09 (P7.20).
- **The rules every view follows stay internal to the core** (`ViewRules`),
  visible to the four view projects, until a view from outside the repository
  appears, so as not to fix an API that may still change. Accepted 2026-10-09
  (P7.21).
- **PixieLib owns the primitives,** consumed from nuget.org. Accepted
  2026-10-09.
- **The ImGui view uses the binding its host uses,** because a binding brings
  its own native library and ImGui context. Today that is Hexa.NET.ImGui 2.2.9
  (ImGui 1.92.2b, docking branch), adopted 2026-10-10. The owner does not want
  to be tied to it: changing it is a decision of this repository and its
  hosts, and PixieEngine is unaffected (2026-10-12).
- **Manual mode serves types without attributes,** such as PixieEngine's
  proxies, where the host creates the members itself. Accepted 2026-10-12.
- **A single source of metadata.** A host whose fields and their metadata
  come from elsewhere, such as PixieEngine's native component descriptors,
  builds the inspector from them with `AddField` and the node options, with no
  hand-written proxy per type; the options are the vocabulary those
  descriptors mirror (`docs/modules/core/REFERENCE.md`). Accepted 2026-10-13
  ([PXI-ARCHIVE-014](docs/archive/ssom-fields-units-arc-2026-10-13.md)).
- **The TuxHost is the versioned verification console;** `hosts/NoHost` is the
  owner's local console and stays in the solution.
- **Documentation in English** under the
  [documentation policy](docs/governance/documentation-policy.md). Accepted
  2026-10-12 ([PXI-ARCHIVE-007](docs/archive/documentation-foundation-2026-10-12.md)).
- **The name is PixieInspector.** Accepted 2026-10-12
  ([PXI-ARCHIVE-005](docs/archive/rename-pixieinspector-2026-10-12.md)).
- **Five packages on nuget.org, at one version:** the core and the four views,
  released together, under the MIT license. Every view is published, used or
  not, so a consumer can pick any of them. A view requires the core of its own
  version exactly, because it sees `ViewRules`. Accepted 2026-10-13
  ([PXI-ARCHIVE-011](docs/archive/nuget-packages-2026-10-13.md)).

## Freezes

None.

## Explicit non-goals

None beyond "What PixieInspector is not". The defects of the 0.7.1a original
listed in section 4 of PXI-ARCHIVE-001 are deliberately not ported.
