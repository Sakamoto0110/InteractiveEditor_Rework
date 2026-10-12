# Editor for Flags Enums

**Document ID:** GLOBAL-PROPOSAL-FLAGS-ENUM

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** an editor that sets and clears the bits of a `[Flags]` enum

**Surface:** proposal

**Boundary:** core

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-007

**State:** idea

**Idea:** An editor for `[Flags]` enums that sets and clears each bit, chosen by
the reflection layer when the enum type carries `FlagsAttribute`.

**Rationale:** A `[Flags]` enum is shown today as a single-choice list of its
defined values, so a combination of bits can be neither picked nor shown as one
of the choices. Verified on 2026-10-12: neither the core nor any view looks at
`FlagsAttribute`. Engines often keep layers and render options as flags.

**Constraints:** Whether PixieEngine has flags enums is not known yet. A new
editor kind is a public API change and needs a decision for each view.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
