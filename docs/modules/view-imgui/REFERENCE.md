# ImGui View Technical Reference

**Document ID:** VIEW-IMGUI-REFERENCE

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** the current technical contract of the Dear ImGui view

**Surface:** technical-reference

**Boundary:** view-imgui

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

The source owns behavior; this document owns the documented contract and was
reconciled against the source at `390c103`. The decisions it cites (`P7.9`)
are in [PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md).

## Mental model

Dear ImGui is immediate mode: nothing persists between frames but what the
code keeps. The view walks the inspector's rows on every frame and shows what
each node holds then, so a change of value, option, visibility, or bind shows
on the next frame without following the nodes' events. It keeps only what
ImGui does not: the text being typed, a drag in progress, the list of choices
while a combo is open. It does not use the core's layout step; ImGui places the
widgets.

## Binding and host

The view is built on Hexa.NET.ImGui 2.2.9 (Dear ImGui 1.92.2b, docking
branch). The host must use the same binding and version, because a binding
brings its own native library and its own ImGui context; a view on one binding
draws into a context the host on another never renders. The view needs nothing
else from the host: no window, renderer, or backend of its own.

## Entry points

| Member | Does |
|---|---|
| `inspector.CreateImGuiView()` | Makes the view of an inspector (P7.2) |
| `DrawWindow()` | Opens an ImGui window titled `Title` and draws the view in it |
| `Draw()` | Draws the view into the current window, for a host that places it itself |
| `Title` | The window's title and ID; `"Label###id"` keeps the ID fixed while the label changes. Defaults to the inspector's name |
| `RefreshEachFrame` | `true` by default: each draw calls `inspector.Refresh()` first, which follows the binder control |
| `Focus(node)` | Gives keyboard focus to the node's editor on the next frame, opening the groups above it |
| `AttachToHost(IImGuiHost)`, `Detach()` | Draws the window on every `Frame` the host raises |
| `Inspector` | The inspector shown; null once it was disposed |
| `Dispose()` | Detaches and lets the inspector go; the inspector is left alone |

`IImGuiHost` has one member, the event `Frame`, which the host raises once per
frame between `ImGui.NewFrame()` and `ImGui.Render()`.

Calling `Draw` or `DrawWindow` outside a frame, or on a thread other than the
one that owns the ImGui context, is the host's mistake: ImGui itself fails.

## What it draws

- A table of two columns: `Member`, 180 wide and fixed, then `Value`,
  stretching; resizable, with lines between the columns and alternating row
  backgrounds. The table's ID is fixed (`##inspector`), so two views in one
  window collide ([PXI-PROPOSAL-005](../../proposals/imgui-view-id-scope.md)).
- A row per node of `inspector.Rows`. A group's label is a tree node that
  opens and closes it (P7.10); the rows inside a collapsed group are passed
  over. The rows are taken before any is drawn, so a write that changes the
  tree (a button that adds a node) shows on the next frame.
- The tooltip of the label, and of the editor, is the node's `Tooltip`, with
  the value's type and the node's path in grey under it. A node with `Help` has a `(?)` at the end of its label, whose
  tooltip is the long help (P7.15). Tooltips show over disabled editors too.
- A read-only node, or one in a disabled branch, draws its editor disabled.
- A failure on the node (`Failure`, or the core's refusal of the last write)
  shows under the editor in light red (P7.11) until a read or a write works.
  What fails while the row draws (a `ToString` of the objects that throws)
  stays in the row, and the next frame tries again (3.11).
- Values are drawn as plain text, never as an ImGui format string, so a `%` in
  a value shows as it is.

## Editors

The editor comes from `ViewRules.KindFor(node)`, shared with the other views.

| Kind | Widget | Writes |
|---|---|---|
| `Text` | A text field; a null shows as a grey `null` hint, told from an empty text | On Enter and when the field is let go; Esc goes back to what the view holds (P2.12) |
| `Number` | ImGui's number field for the integer and floating types, with step buttons for the integers | As it is typed, through the node's rules |
| `Slider` | A slider over `Range`, in double, rounded to `Range.Step` when there is one | While it is dragged |
| `Toggle` | A check box | At once |
| `Choice` | A combo box filled from `GetChoices()` when it opens (P6.3) | At once |
| `Color` | ImGui's color field: a swatch that opens a picker, and the RGBA inputs | While it is dragged; channels from 0 to 1, rounded to bytes |
| `Selector` | A combo box with the collection's items | Sets `SelectedIndex`, which is not an edit, so it works on a read-only collection |
| `List` | A list box of the items, up to `ListRows` lines, and four buttons: add, remove, up, down | The core's list operations, at once (P5.13) |
| `Button` | A button across the column with the node's `Text` | Calls `Press()` |
| `Display` | Grey text that wraps; null reads `null` | Never |
| `Header` | The group of a type hidden with `new` (P5.9): its label only | Never |
| `Separator` | A line across the row | Never |

- A number the number field cannot show (a nullable number, a `decimal`)
  goes to the text field; an empty text is null for a nullable member.
- A text the core refuses stays as it was typed, with the failure under it,
  until the value changes; the object keeps its value.
- With `EnterReturnsTrue`, ImGui 1.92 hands the text back only when the field
  is let go, and on Esc it hands back what was typed; the text editor treats a
  field let go on Esc as a cancel.
- The color field takes a `PxColorRgba`, a `PxColorHsl`, or a
  `System.Drawing.Color`.

## Mixed values and scrubbing

- Objects that hold different values (`ViewRules.ShowsMixed`) show `--`
  (ImGui's default font has no em dash): the text field shows none with a grey
  dash, the check box is indeterminate, the combo has nothing chosen, the
  color shows a dash and opens on the first object's color. A value set then
  goes to all of them (P7.19).
- A number with a `ScrubMultiplier` scrubs from its label (P7.18): dragging
  past a few pixels moves the value by the distance times the multiplier,
  across or up and down (`ScrubAxis`), and writes while it drags. Each object
  moves by the same delta from where it started (`SetValues`, P7.16); Shift
  makes the step ten times larger, Ctrl ten times smaller, and Esc puts every
  object back. A row that scrubs shows the first object's value instead of the
  dash.

## Lifecycle, errors, and threading

- Disposing the view leaves the inspector alone; when the inspector is
  disposed, the view empties and draws nothing (P7.13).
- The view has no escape valves and no `RowFailed`: there are no controls to
  hand out, and its failures stay in the row.
- An exception of a subscriber of the inspector's events goes up through the
  view, as one of the inspector's events does (3.11), with ImGui's `Begin` and
  `End` pairs closed on the way, so a host that catches it can go on with the
  frame.
- The view reads and writes the bound objects on the thread that draws. With
  `RefreshEachFrame` on, each frame reads every node; a host whose objects
  report their changes, or that refreshes on its own schedule, turns it off.

## Conversions

`PrimitiveConversions` turns the PixieLib primitives into the
`System.Numerics` types ImGui works with: positions and sizes into `Vector2`
(narrowed to `float`), colors into RGBA `Vector4` with components from 0 to 1.

## Validation expectations

The [ImGuiHost](../../../hosts/ImGuiHost/README.md) shows every demo object.
A headless harness that drives the view through ImGui's input queue (25
scenarios) validated the port to Hexa.NET.ImGui; it lives outside the
repository ([PXI-PROPOSAL-003](../../proposals/imgui-harness.md)). Headless
runs must accept the renderer's textures (`RendererHasTextures`) and keep the
navigation cursor visible. Label scrubbing needs a real mouse and was not part
of that harness.
