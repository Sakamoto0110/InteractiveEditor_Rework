# Dropping References onto a Row

**Document ID:** GLOBAL-PROPOSAL-REFERENCE-DROP

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** a host extension point to drag entity and asset references onto a row

**Surface:** proposal

**Boundary:** view-imgui

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-006

**State:** idea

**Idea:** An extension point in the ImGui view through which the host accepts a
drag payload on a row: an entity dragged from the scene tree, or an asset
dragged from the asset manager, sets the member it is dropped on. Choosing from
a list already works through the node's `Choices` (P6.3).

**Rationale:** PixieEngine lists entity and asset references among the types its
editor needs, with both a selector and dragging. Only the host knows what an
entity or an asset is and what its drag payload looks like, so the inspector
should offer the hook and not the meaning.

**Constraints:** It depends on how the engine represents a reference in C#,
which PixieEngine has not decided. A design is premature until then.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
