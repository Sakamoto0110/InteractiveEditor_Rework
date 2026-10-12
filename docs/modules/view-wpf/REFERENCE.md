# WPF View Technical Reference

**Document ID:** VIEW-WPF-REFERENCE

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** the current technical contract of the WPF view

**Surface:** technical-reference

**Boundary:** view-wpf

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

The source owns behavior; this document owns the documented contract and was
reconciled against the source at `390c103`. The decisions it cites (`P7.9`)
are in [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md),
section 0 (Views) and section 3.5.

## Mental model

The WPF view is the [WinForms view](../view-winforms/REFERENCE.md) on the other
platform (P7.7): it receives an inspector and observes it, with no state of its
own about the objects, puts the rows on the rectangles of the core's layout
step, a canvas per group (P7.3), inside a scroll viewer, and shows whatever the
inspector reports. Changes are laid out once, when the UI thread gets to it,
and a relayout keeps a row's controls while its node keeps the same editor.

## Entry points

| Member | Does |
|---|---|
| `inspector.CreateWpfView()` | Makes the view of an inspector (P7.2) |
| `WpfInspectorView` | A `UserControl` that is also `IDisposable`, since a WPF control has no `Dispose` |
| `Inspector` | The inspector shown; null once it was disposed |
| `ControlCreated`, `RowCreated` | The escape valves (P7.4), with `FrameworkElement` controls |
| `RowFailed` | What failed in the view itself (3.11) |
| `Dispose()` | Lets the inspector go and leaves it alone |

The rows are made when the view is loaded, with the width it got, so a
subscriber to the valves right after `CreateWpfView` sees every control.

## Differences from the WinForms view

Everything the WinForms reference says about the rows, the help mark and its
window, the hover background, the tooltips and failures, the editors, the
mixed values, scrubbing, the escape valves, `RowFailed`, and the UI thread
holds here, with these differences:

- **Slider.** A WPF slider counts in double, so it takes the range as it is,
  stopping on the step when there is one.
- **Color.** WPF has no color dialog of its own, so the color button opens the
  WinForms `ColorDialog`, with the view's window as its owner. The project
  references WinForms for it.
- **Mixed text.** The em dash shows in a text block over the empty text box,
  and the check box shows its third, null state.
- **Separator.** A dark line over a light one, as an etched edge.
- **Automation.** The editor's `AutomationId` is its node's path, the label's
  the path with `#label`, and the help mark's the path with `#help`, since a WPF
  `Name` cannot hold the dots of a path; `Tag` stays free for the application.
- **UI thread.** What comes from another thread is marshalled through the
  view's `Dispatcher` (P7.12).

## Conversions

`PrimitiveConversions` converts the PixieLib primitives to WPF types, as
extensions rather than implicit operators, so neither PixieLib nor the core
depends on WPF.

## Platform and validation

`net10.0-windows`, Windows only. The [WpfHost](../../../hosts/WpfHost/README.md)
shows a `Gadget` with every editor. On Linux the view runs under Wine, with
`fonts-wine`, for verification; a console that drove it there (139 checks)
lived outside the repository, and the final look, with its theme and fonts, is
checked on Windows.
