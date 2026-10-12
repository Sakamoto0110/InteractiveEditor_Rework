# PixieInspector Promoted Work

**Document ID:** GLOBAL-TODO

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** formally promoted work, gates, dependencies, and completion criteria

**Surface:** work-scheduler

**Boundary:** global

**Authority role:** scheduler

**Mutation:** authored

**Indexing:** include

**Last promotion decision:** 2026-10-12

## Purpose and admission rule

This file is the only scheduler. It holds work that the owner has formally
promoted, with enough scope to execute and close it. It does not hold ideas
([`docs/proposals/`](docs/proposals/README.md)), direction
([`ROADMAP.md`](ROADMAP.md)), or completion narratives
([`docs/archive/`](docs/archive/README.md)).

An entry whose scope is large, or that decomposes into many small tasks, keeps
one entry here and its breakdown in [`TASKLIST.md`](TASKLIST.md). The rules are
in the [documentation policy](docs/governance/documentation-policy.md#scheduling-todomd-and-tasklistmd).

Unchecked or `promoted` entries are not automatically authorized to start; an
entry may keep an explicit gate after promotion.

## Current promoted work

### `PXI-TODO-001` — Adopt the documentation policy

**Status:** in-progress

**Source:** owner decision on 2026-10-12, accepting the migration map of the
`inventory/design` step (questions 1 to 6)

**Accepted scope:** the documentation policy, schema, and templates derived
from PixieEngine's; the four documents written before it archived whole, in
Portuguese, as imported records; the root documents, the development guide,
and a `README.md` per host; the module documents (`MANIFEST.md`, `README.md`,
`REFERENCE.md`) of `core`, `view-imgui`, `view-avalonia`, `view-winforms`, and
`view-wpf`, written from the current source; the eight ideas of the PixieEngine
editor discussion as proposals. Out of scope: any change to product code or
public API, and the promotion of any proposal.

**Dependencies and gates:**

1. The owner accepts the migration map. Met on 2026-10-12.

**Completion criteria:**

- Every module has its three required surfaces, and every current claim in
  them was checked against the source.
- No current fact lives only in the archive.
- Each module passes the self-review: metadata, links and anchors, one
  normative reference, history kept as snapshots, proposals not presented as
  facts.
- The solution still builds with 0 warnings and 0 errors.

**Breakdown:** [`TASKLIST.md`](TASKLIST.md#pxi-todo-001--adopt-the-documentation-policy)
