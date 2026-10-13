# ImGui Mixed Label

**Document ID:** ARCHIVE-IMGUI-MIXED-LABEL-20261013

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the ImGui view showing no mark of different values on a row that scrubs, and the tinted label that marks it

**Surface:** archive-record

**Boundary:** view-imgui

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-015

**Type:** fix

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-13

**Commits:** `61ea9cbfd7b447f350b43d5780087395a206d3eb`, `32054d91ff04012d0f6a3433fd0760a9d33d3476`

**Current state:** [`docs/modules/view-imgui/REFERENCE.md`](../modules/view-imgui/REFERENCE.md#mixed-values-and-scrubbing)

PixieEngine's session, working on the owner's request for multi-selection
(the criterion: different values show as mixed), reported that with 0.2.0 two
entities with different positions showed the first one's `x` and no mark.
Every number of PixieEngine's editor scrubs.

The behavior followed P2.4: a number that scrubs shows the first object's
value, since the drag moves each object from its own. The owner's decision of
2026-10-02 ([PXI-ARCHIVE-001](modernization-notes-2026-10-10.md), P7.19)
completed it: the label stays italic, so the row does not read as a value all
of them hold. The WinForms, WPF, and Avalonia views do that. ImGui's default
font has no italic, and the ImGui view had no mark at all, so this was a gap
in the view, not a new design.

The fix: the label of a row that `ViewRules.ShowsMixed` is drawn in amber
(`MixedColor`), for every editor kind, and its tooltip says the objects differ
and, on a row that scrubs, that the value shown is the first one's. Released
as 0.2.1.

Verified: the solution builds in Release with warnings as errors; a scratch
ImGui host rendered a `Gadget` view with two objects bound, and the rows whose
objects differ (`Count` and `Ratio`, which scrub, the `Direction` arc, and
`Spread`, which shows `--`) have amber labels while the others do not.
