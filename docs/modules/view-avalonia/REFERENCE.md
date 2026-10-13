# Avalonia View Technical Reference

**Document ID:** VIEW-AVALONIA-REFERENCE

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** the current technical contract of the Avalonia view

**Surface:** technical-reference

**Boundary:** view-avalonia

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

The source owns behavior; this document owns the documented contract and was
reconciled against the source at `390c103`. The decisions it cites (`P7.9`)
are in [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md),
section 0 (Views) and section 3.5.

## Mental model

The Avalonia view is a property grid: a row for each node the inspector shows
(`Rows`), the label in a column on the left and the editor on the right, and an
expander for each group, with the label and the type in its header and the
group's rows inside (P7.3). Avalonia's own panels lay the rows out, so the
rectangles of the core's layout step are not used. Like the WinForms and WPF
views, it observes the inspector with no state of its own about the objects:
a value that changed shows on its row, and whatever changes the rows (an
option, the bind, a rule of visibility) builds them again, once per burst,
keeping a row's controls while its node keeps the same editor.

## Entry points

| Member | Does |
|---|---|
| `inspector.CreateAvaloniaView()` | Makes the view of an inspector (P7.2) |
| `AvaloniaInspectorView` | A `UserControl` that is also `IDisposable`, since an Avalonia control has no `Dispose` |
| `Inspector` | The inspector shown; null once it was disposed |
| `ControlCreated`, `RowCreated` | The escape valves (P7.4), with Avalonia `Control`s |
| `RowFailed` | What failed in the view itself (3.11) |
| `Dispose()` | Lets the inspector go and leaves it alone |

The rows are made when the view is loaded, so a subscriber to the valves right
after `CreateAvaloniaView` sees every control. A test that creates the view
must put it in a window and let the UI queue run before looking for controls.

## Editors

The editor comes from `ViewRules.KindFor(node)`, shared with the other views.

| Kind | Control | Writes |
|---|---|---|
| `Text` | A text box; a null shows as a `null` placeholder | On Enter and when it loses the focus; Esc goes back to what the view holds (P2.12) |
| `Number` | A spinner for the integer types and `decimal`, stopping at the range or the type's limits and stepping by the range's step; `float` and `double` go to a text box | As it spins, or as text |
| `Slider` | A slider over `Range`, in double, stopping on the step, with the value as text beside it | While it is dragged |
| `Toggle` | A check box; a nullable `bool` gets the third state, for null | At once |
| `Choice` | A combo box filled from `GetChoices()` whenever it opens (P6.3) | At once |
| `Color` | A swatch with the color's code; a click opens a flyout with a slider per channel, alpha included | While the sliders move |
| `Selector` | A combo box with the collection's items, on top of the collection's group | Sets `SelectedIndex` |
| `List` | A list box of the items, up to `ListRows` lines, and four buttons under it | The core's list operations, at once (P5.13) |
| `Button` | A button with the node's `Text` | Calls `Press()` |
| `Display` | Text that can be selected and copied; null reads `null` | Never |
| `Separator` | The theme's separator across the row | Never |

Avalonia has no color dialog of its own (its color picker is a separate
package), hence the flyout.

- An `Arc` shows its center as a number (no dial); its width keeps its own
  row. A number in a `Unit` is shown and typed in that unit, which the core
  converts, without the unit's suffix.

## Rows

- A node with `Help` has a `(?)` mark in the theme's color for secondary text,
  with the hand cursor; a click opens a modal window, titled with the row's
  label, whose text scrolls and can be selected; OK, Enter, and Esc close it
  (P7.15). In a group's header the mark takes the press, so the click opens the
  help and not the expander.
- Type names in the headers and tooltips read as C# writes them (`Int32?`,
  `List<Moo>`).
- Mixed values turn the label italic and show an em dash (`—`) as placeholder
  or text, and the check box its null state; a value set then goes to all of
  them (P7.19). A number with a
  `ScrubMultiplier` scrubs from its label, as in the WinForms view (P7.18).
- A failure shows under the editor, with its message, in the theme's error color.

## Escape valves, failures, lifecycle, and threading

- `ControlCreated` and `RowCreated` behave as in the
  [WinForms view](../view-winforms/REFERENCE.md#escape-valves-and-failures),
  and come again for a row made anew: a node that changed its editor, or one
  that shows again after it was hidden.
- `RowFailed`: a row that could not be made is left out with its branch until
  the next build (`Critical`); one that could not show its objects keeps what it
  showed, with the message (`WorkedAround`). A valve that throws stops the
  build there; the view stays usable.
- Disposing the view lets the inspector go; when the inspector is disposed,
  the view empties (P7.13).
- What comes from another thread is posted to Avalonia's UI thread first
  (`Dispatcher.UIThread`, P7.12).

## Conversions

`PrimitiveConversions` converts the PixieLib primitives to Avalonia types, as
extensions rather than implicit operators, so neither PixieLib nor the core
depends on Avalonia.

## Platform and validation

`net10.0` on Avalonia 12.1.3; it runs on Windows, Linux, and macOS. The
[AvaloniaHost](../../../hosts/AvaloniaHost/README.md) shows every demo object.
A console that drove the view on a virtual display (106 checks: the editors,
groups, mixed values, failures, the help mark, the valves, and scrubbing)
lived outside the repository. Simulated pointer events need a test-only
`Avalonia.Input.Pointer`, and `xdotool` gives a real mouse to compare with.
