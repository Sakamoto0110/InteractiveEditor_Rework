# Views Confirmed on Windows

**Document ID:** ARCHIVE-VIEWS-CONFIRMED-20261012

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the owner's check of the hosts on Windows and confirmation of the views' fourth- and fifth-cut choices

**Surface:** archive-record

**Boundary:** global

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-010

**Type:** decision

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-12

**Commits:** with this record (parent `693dacf2482f353cae63fcbc928c56195c56b214`)

**Reference commit:** `f2714d46004b569d0efe209acc473c8d1effac4d`, the code the owner checked

**Current state:** [`docs/modules/view-winforms/REFERENCE.md`](../modules/view-winforms/REFERENCE.md), [`docs/modules/view-wpf/REFERENCE.md`](../modules/view-wpf/REFERENCE.md)

Two questions had been open since the handoff of 2026-10-10
([PXI-ARCHIVE-002](context-handoff-2026-10-10.md), section 7), and the
completion record of the documentation repeated them
([PXI-ARCHIVE-007](documentation-foundation-2026-10-12.md)). The owner
answered both with "yes" on 2026-10-12:

1. **The hosts on Windows.** The WindowsHost and the WpfHost look and behave
   right there: the spacer and the maximum width, the `(?)` mark and its
   window, the hover background, scrubbing `Count` across and `Ratio` up and
   down, the mixed values after binding a second gadget, and `Count` and
   `Ratio` aligned right by the escape valve; and so do the AvaloniaHost, the
   ImGuiHost, and the TerminalHost. This is the owner's interactive check, the
   evidence the Wine runs could not give for fonts and themes.
2. **The choices taken without asking** in the views' fourth and fifth cuts
   stay: four view events with generic arguments in the core; rows made when
   the view gets its window; an exception of a valve's subscriber going up like
   an inspector event; `RowFailed` critical for a row that cannot be made and
   worked-around for one that cannot show its objects; the WPF view
   disposable; the node's path as the WPF automation ID; WinForms' color dialog
   in WPF; the em dash over the empty text box; the WPF slider in double; and
   the rules the views share in `ViewRules`.

The view references already describe these choices as current; nothing in
them changes.
