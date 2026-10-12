# PixieInspector Task List

**Document ID:** GLOBAL-TASKLIST

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** task breakdowns of promoted TODO.md entries

**Surface:** task-scheduler

**Boundary:** global

**Authority role:** scheduler

**Mutation:** authored

**Indexing:** include

## Purpose

This file is a child scheduler of [`TODO.md`](TODO.md). Each section breaks
one promoted `TODO.md` entry into tasks, so that large entries, or entries made
of many small tasks, do not pollute `TODO.md`.

- Every section names its parent in `Parent`, and the parent links back here
  in `Breakdown`.
- Tasks decompose the parent's accepted scope and never extend it.
- When the parent completes, its section moves to
  [`docs/archive/`](docs/archive/README.md).

The full rules are in the
[documentation policy](docs/governance/documentation-policy.md#scheduling-todomd-and-tasklistmd),
and the section shape is in the
[template](docs/templates/TASKLIST-SECTION.md).

## Open breakdowns

## `PXI-TODO-001` — Adopt the documentation policy

**Parent:** PXI-TODO-001

- `PXI-TASK-001` — **Status:** done — Archive the four documents written before
  the policy, whole, as PXI-ARCHIVE-001 to 004 (`997c70d`).
- `PXI-TASK-002` — **Status:** done — The policy, schema, templates, archive
  router, and the eight proposals (`985e597`).
- `PXI-TASK-003` — **Status:** in-progress — The root documents, the
  development guide, and a `README.md` per host.
- `PXI-TASK-004` — **Status:** open — The module documents of `core`.
- `PXI-TASK-005` — **Status:** open — The module documents of `view-imgui`.
- `PXI-TASK-006` — **Status:** open — The module documents of `view-avalonia`,
  `view-winforms`, and `view-wpf`.
- `PXI-TASK-007` — **Status:** open — The self-review of each module, then the
  completion record.
