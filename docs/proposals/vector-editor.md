# One-Row Vector Editor

**Document ID:** GLOBAL-PROPOSAL-VECTOR-EDITOR

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** an editor that shows a vector, point, or size on one row

**Surface:** proposal

**Boundary:** core

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-002

**State:** idea

**Idea:** A new editor kind for vectors, points, and sizes: one row with one
numeric field per component, side by side, each with label scrubbing. The
reflection layer chooses it for the PixieLib `PxVec*`, `PxPoint*`, and `PxSize*`
types and for the `System.Numerics` vectors, the way it chooses the number
editor for a `float`. The ImGui view draws it first; the other views follow by
their own decision.

**Rationale:** A vector is shown today as a group with one row per field. In
manual mode each vector costs three `Add` calls and three rows. PixieEngine
lists vectors, points, and sizes first among the types its editor needs, and a
transform becomes three compact rows (position, rotation in degrees, scale) once
the proxy exposes them as vectors.

**Constraints:** A new `EditorKind` is a public API change in the core and needs
a decision for each view. Partial edits must write the whole struct back through
the parent, as the core already does for a field of a struct. Mixed values and
ranges must apply per component.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
