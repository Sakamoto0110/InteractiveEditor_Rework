# Manual Add Without the Editor Warning

**Document ID:** GLOBAL-PROPOSAL-MANUAL-ADD-WARNING

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** current

**Subject:** whether a member added by hand should warn that its editor is unchosen

**Surface:** proposal

**Boundary:** core

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

## PXI-PROPOSAL-004

**State:** idea

**Idea:** `Add(name)` stops raising the P5.8 warning ("shows as Display until an
editor is chosen") for a member whose type fits more than one editor, such as a
color. The member still shows as `Display` until the manual layer chooses an
editor.

**Rationale:** The warning exists for automatic discovery, where nobody chooses
next. After `Add`, the next step is the manual layer itself, which chooses the
editor on the following line. Verified on 2026-10-12: adding a `PxColorRgba`
member by hand puts a `WorkedAround` failure in the report and raises
`DiscoveryFailed` before the host sets `Editor`. In the PixieEngine editor,
every color on every proxy would put a false warning in the status bar.

**Constraints:** The report would then say nothing about a member a host adds
and never configures. The behavior of the automatic `Create` does not change.

**Promotion target:** owner decision required

**Source:** owner discussion of the PixieEngine editor on 2026-10-11 and
2026-10-12, recorded in
[PXI-ARCHIVE-008](../archive/editor-discussion-2026-10-12.md)
