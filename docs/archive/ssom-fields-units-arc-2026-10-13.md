# Single Source of Metadata: Fields, Units, and Arcs

**Document ID:** ARCHIVE-SSOM-FIELDS-UNITS-ARC-20261013

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the inspector's side of a single source of metadata for PixieEngine's editor: fields with their own reader and writer, display units, and the arc editor, released as 0.2.0

**Surface:** archive-record

**Boundary:** global

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-014

**Type:** decision

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-13

**Commits:** `34e326994a5b6d5aa8d5afcf1016a0b089359e20`, `e7989d7809508951284e078ef8be8b3da899ea16`, `1ee866179fb26174531d5b394f310e01a35d4a71`, `d9d7b8b44689914a42e7f04a6784da93c2d27046`

**Current state:** [`docs/modules/core/REFERENCE.md`](../modules/core/REFERENCE.md), [`docs/modules/view-imgui/REFERENCE.md`](../modules/view-imgui/REFERENCE.md)

## What the owner found

Reviewing PixieEngine's editor on 2026-10-13, the owner missed better
tooltips, scrubbing on more fields, multi-bind (several attractors edited at
once), more start values for the scenarios, a particle spawner separate from
the particle system with a better way to aim it, and an attractor that
behaves as a body of its size rather than a point.

Reading the engine's code showed why the editor's metadata was thin: each
component was described three times, by hand, in the C++ struct (units only
in comments), in the native registration (`ComponentTypes`, names as text),
and in a C# proxy per component (names again, plus tooltips and ranges). The
proxies also carried inspector attributes, against the owner's decision of
2026-10-12 that proxies carry none and the host builds inspectors in manual
mode ([PXI-ARCHIVE-008](editor-discussion-2026-10-12.md)). Multi-bind,
mixed values, and scrubbing by the same delta already existed in the core;
the editor had no multi-selection to use them.

## Decisions

The owner accepted every recommendation, with "a single source of metadata,
even if it costs more":

1. The source is the native registration, extended with metadata now, and
   C++26 annotations later, when MSVC implements reflection; they would
   produce the same descriptors.
2. The editor reads the descriptors at run time and builds inspectors from
   them, with no proxy per component, so a game library's own components need
   no editor rebuild. The core gains fields with their own reader and writer
   for this.
3. Ranges and scrubbing steps are written in the unit shown.
4. An arc is a pair of fields, a center and a width, not a new PixieLib type.
5. The core's node options are the metadata vocabulary; the engine's
   descriptors mirror them.

The owner also asked for a display unit (radians held, degrees shown) and an
arc of possible directions, for the particle spawner.

## What was built

- **`AddField`** (`FieldNode`): a member node with no `MemberInfo`, read and
  written through the host's delegates on the object it sits in, in each
  bound object. Everything a member has works: options, rules, events,
  several bound objects, mixed values; no writer makes it read-only; what
  the delegates throw is reported on the node.
- **`Unit`** (`DisplayUnit`, `DisplayUnit.Degrees`): the view's values, the
  values written, text, the range, and scrubbing are in the unit, as a
  `double` (`ViewType`); `GetValue` reads the member. The conversion lives in
  the core, so every view shows and takes the unit; only the ImGui view shows
  its suffix.
- **`EditorKind.Arc`** with `ArcSpread`: the ImGui view draws a dial with the
  sector and a field for each angle; drag turns the center, Shift and drag
  opens the width. The other views show the center as a number, a choice the
  owner can revisit per view.
- `[InspectorUnit]` and `[InspectorArc]`, and `Gadget.Direction` and
  `Gadget.Spread` in the demo objects.
- Version 0.2.0.

## Verification

On Windows, .NET SDK 10.0.401:

- A scratch console outside the repository ran 25 checks of fields, units,
  and arcs, all passing. Breaking the unit conversion on purpose failed 5 of
  them, and removing the wrapping of a writer's exception crashed the
  console; both were restored.
- The solution builds in Release with warnings as errors: 0 warnings.
- The TuxHost output is byte-identical to `main` (67 lines). `TerminalHost
  --dump` exits 0, with the two new `Gadget` rows (`Direction = 90 [Arc]`,
  `Spread = 60 [Number]`) and 36 of 36 bindings ok.
- A scratch ImGui host rendered the `Gadget` view offscreen to PNG: the dial
  with a 60° sector facing up and the fields `90 °` and `60 °`; with two
  objects bound, `Spread` shows `--` and the arc shows the first object's
  values. Mouse events injected through ImGui's input queue turned the center
  to the drag (about 0°) and, with Shift, opened the width to 164°.

Not covered: the arc in the WinForms, WPF, and Avalonia views, which show a
number by design, and PixieEngine's side of the descriptors, which is that
repository's work.
