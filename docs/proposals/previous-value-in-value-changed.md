# The Previous Value in ValueChanged

**Document ID:** GLOBAL-PROPOSAL-PREVIOUS-VALUE

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** carrying the value before a change in `ValueChangedEventArgs`, for undo

**Surface:** proposal

**Boundary:** core

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-008

**State:** idea

**Idea:** `ValueChangedEventArgs` carries the values before the change, one per
bound object, next to the node and the source.

**Rationale:** An editor needs undo. Today the event carries only the node and
the source (`ValueSource`), so a host has to keep its own copy of every value to
know what to restore.

**Constraints:** For a change found by reading (an outside change), the previous
value is the one the node last saw, not necessarily the true old value.
Multi-bind needs one value per object.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
