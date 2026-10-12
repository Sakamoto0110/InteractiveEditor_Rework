# WinForms View Technical Reference

**Document ID:** VIEW-WINFORMS-REFERENCE

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** the current technical contract of the WinForms view

**Surface:** technical-reference

**Boundary:** view-winforms

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

The source owns behavior; this document owns the documented contract and was
reconciled against the source at `390c103`. The decisions it cites (`P7.9`)
are in [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md),
section 0 (Views) and section 3.5.

## Mental model

The view receives an inspector and observes it, with no state of its own about
the objects (P7.7). Its rows go on the rectangles of the core's layout step,
with a panel per group (P7.3), and whatever the inspector reports (a value, an
option, the bind, a failure) shows. Changes come in bursts, so they are laid
out once, when the UI thread gets to it; a relayout keeps the controls of a row
while its node keeps the same editor, so it takes neither the focus nor the
text being typed.

## Entry points

| Member | Does |
|---|---|
| `inspector.CreateWinFormsView()` | Makes the view of an inspector (P7.2) |
| `WinFormsInspectorView` | A `UserControl`; place it like any other |
| `Inspector` | The inspector shown; null once it was disposed |
| `ControlCreated`, `RowCreated` | The escape valves (P7.4) |
| `RowFailed` | What failed in the view itself (3.11) |

The rows are made when the view gets its window, so a subscriber to the valves
right after `CreateWinFormsView` sees every control. The editors take the
view's width; the rows keep their height.

## What it draws

- The label in a column on the left, an optional `(?)` help mark, and the
  editor on the rest of the row, on the layout's rectangles; the editors line
  up and stop at `EditorMaxWidth` (P7.14).
- A group's label carries a drawn arrow that collapses it (P7.10); its panel
  hides while it is collapsed.
- The row under the mouse gets a light background, a little of the system's
  highlight over the background, so it follows the system's colors (P7.14).
- A node with `Help` has a `(?)` mark, drawn on one line so it fits its narrow
  column; a click opens a modal window, titled with the row's label, whose text
  scrolls and can be selected; OK, Enter, and Esc close it (P7.15).
- The tooltip of a row is the node's `Tooltip`, or the row's failure (P7.11),
  and a failure paints the editor's background light red.
- A read-only node, or one in a disabled branch, shows its editor disabled.

## Editors

The editor comes from `ViewRules.KindFor(node)`, shared with the other views.

| Kind | Control | Writes |
|---|---|---|
| `Text`, `Number` | A text box | On Enter and when it loses the focus; Esc goes back to what the view holds (P2.12) |
| `Display` | A read-only text box | Never |
| `Slider` | A track bar over `Range`; its positions go by the step, or a hundred with no step | While it is dragged |
| `Toggle` | A check box | At once |
| `Choice` | A combo box filled from `GetChoices()` when it opens or takes the focus (P6.3) | At once |
| `Color` | A button painted with the color, which opens the system's `ColorDialog` | When the dialog closes with OK; the dialog has no alpha, so the alpha stays |
| `Selector` | A combo box with the collection's items | Sets `SelectedIndex` |
| `List` | A list box of the items and four buttons on the line below | The core's list operations, at once (P5.13) |
| `Button` | A button with the node's `Text` | Calls `Press()` |
| `Separator` | A horizontal line across the row | Never |

A text the core refuses stays as it was typed, with the failure, until the
value changes; the object keeps its value. The color button takes a
`System.Drawing.Color`, a `PxColorRgba`, or a `PxColorHsl`.

## Mixed values and scrubbing

- Objects that hold different values (`ViewRules.ShowsMixed`) turn the row's
  label italic and show an em dash (`—`): as the text box's placeholder, on the
  color button; the check box is indeterminate. A value set then goes to all of
  them (P7.19).
- A number with a `ScrubMultiplier` scrubs from its label (P7.18): dragging
  moves the value by the distance times the multiplier, across or up and down
  (`ScrubAxis`), and writes while it drags; each object moves by the same delta
  from where it started (P7.16).

## Escape valves and failures

- `ControlCreated` hands out each control the view made for a node, with its
  `RowPart` (`Label`, `Help`, `Editor`, `Panel`); `RowCreated` then hands out
  the whole row, placed and showing its value. Each comes once, and again only
  for a row made anew, when its node changed its editor. A host uses them for
  what the agnostic options do not cover, such as aligning a number to the
  right (P7.4).
- An exception of a valve's subscriber goes up through the view, as one of the
  inspector's events does (3.11); the layout stops there, the view stays
  usable, and the next change lays out the rest.
- `RowFailed` reports what failed in the view itself: a row that could not be
  made or placed is left out until the next layout (`Critical`); one that could
  not show its objects keeps what it showed, in light red (`WorkedAround`).

## Lifecycle and threading

- Disposing the view leaves the inspector alone; when the inspector is
  disposed, the view empties (P7.13).
- What the inspector reports from another thread moves to the UI thread first
  (`BeginInvoke`, P7.12).

## Conversions

`PrimitiveConversions` converts the PixieLib primitives to the WinForms types
PixieLib does not know (PixieLib converts its double types and colors to
`System.Drawing` itself), as extensions, so neither PixieLib nor the core
depends on WinForms. `PxDockConversions` converts `PxDock` to and from
`DockStyle`.

## Platform and validation

`net10.0-windows`, Windows only. The [WindowsHost](../../../hosts/WindowsHost/README.md)
shows a `Gadget` with every editor. On Linux the view runs under Wine for
verification; consoles that drove it there (136 checks) lived outside the
repository, and the final look is checked on Windows.
