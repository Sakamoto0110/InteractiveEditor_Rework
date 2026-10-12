# PixieLib 0.2.0

**Document ID:** ARCHIVE-PIXIELIB-020-20261012

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the move from PixieLib 0.1.0 to 0.2.0, and the compatibility check behind it

**Surface:** archive-record

**Boundary:** core

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-009

**Type:** dependency

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-12

**Commits:** `693dacf2482f353cae63fcbc928c56195c56b214`

**Current state:** [`DEPENDENCIES.md`](../../DEPENDENCIES.md#own-projects)

The owner published PixieLib 0.2.0 on nuget.org and asked whether
PixieInspector is compatible with it.

## What changed in PixieLib

The public surface of the two `net10.0` assemblies was dumped by reflection and
compared member by member:

- **No type removed, renamed, or changed in layout.** Fields and constructors
  are the same, and the unsuffixed types stay `double` in C#.
- **Added:** `PxSegment`, `PxSegmentf`, and `PxSegmenti`; `Lerp` on the points;
  lengths and distances on `PxVec2i`; a 3x3 matrix times a point.
- **Removed, the breaking part:** the point arithmetic that depends on where
  the origin is. `point + point`, `point * k`, `k * point`, `point / k`, and
  `-point` are gone from `PxPoint`, `PxPointf`, and `PxPointi`, and a rectangle
  no longer moves by a point. In their place, `point - point` is a vector, and a
  point or a rectangle moves by a vector. The package's README states the rule.

## Compatibility

Nothing in the core, the views, or the hosts used a removed operator. With the
reference at 0.2.0, in a separate worktree first:

- the solution built with 0 warnings and 0 errors;
- the TuxHost output (67 lines) and the TerminalHost `--dump` output (7
  targets, all `ok`) were byte-identical to those on 0.1.0;
- the out-of-repository parity console (28 cases, each created three times)
  printed the same tree, options, and report;
- the unloadable-context test still collected its context;
- the headless ImGui harness passed 25 of 25 scenarios, color editing included.

Each check restored 0.2.0, confirmed in its `project.assets.json`. The
reference moved to 0.2.0 in the commit above. Code written against PixieLib
outside this repository, such as a host's own arithmetic on points, is not
covered by these checks; the development guide notes the new rule.
