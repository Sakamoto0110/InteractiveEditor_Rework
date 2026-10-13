# Core Technical Reference

**Document ID:** CORE-REFERENCE

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** the current technical contract of the core: the inspector, its nodes, options, binding, errors, and layout

**Surface:** technical-reference

**Boundary:** core

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

The source owns behavior; this document owns the documented contract and was
reconciled against the source at `390c103`. Numbers such as `P5.10` and `3.11`
point to the decisions and sections of
[PXI-ARCHIVE-001](../../archive/modernization-notes-2026-10-10.md), where the
rationale lives.

## Mental model

An `Inspector` holds a tree of nodes built for one type, and the objects bound
to it. Each node knows its path from the root (`"Moo.MooX"`), its options, and
how to reach its value in a bound object: through its parent, one level at a
time, from the bound object down. Nothing below the root keeps a reference to
the objects it reads, so a replaced object or a struct copied on the way is
always read fresh, and a struct written in the middle of a chain is written
back to its owner, up to the bound object.

A view walks the tree (`Rows`, or `ShownChildren` a level at a time), makes a
row per node, and calls the node to read and write. The core decides
everything a row needs: the editor, whether it is read-only, whether it shows,
whether its objects hold different values, and the rectangles of a layout.

## Architecture and boundary

| Area | Source | Holds |
|---|---|---|
| The inspector | `Inspector.cs`, `RootNode.cs` | Creation, binding, the binder's flows, the report, the events |
| Nodes | `InspectorNode.cs`, `MemberNode.cs`, `CollectionNode.cs`, `ItemNode.cs`, `TypeGroupNode.cs`, `ButtonNode.cs`, `DisplayNode.cs` | The tree, the options of each node, reading and writing |
| Discovery | `Model/ReflectionDiscovery.cs`, `Model/TypeModel.cs`, `Model/DeclarationOrder.cs` | Building the tree from a type |
| Options | `Options/` | The layers of options, the global and per-inspector options, the editor kinds, rules, ranges |
| Binding helpers | `Model/ValueConverter.cs`, `Model/InstanceWatcher.cs`, `Model/MemberPath.cs` | Conversion, change notifications, expression selectors |
| Diagnostics and events | `Diagnostics/`, `Events/` | Failures, the report, event arguments |
| Layout | `Layout/` | The layout step (P7.5) |
| View rules | `Views/ViewRules.cs` | What every view decides the same way (internal) |
| Attributes | `Attributes/` | The ten `[Inspector*]` attributes |

The core targets `net10.0`, references PixieLib for its primitives
(`PxRect`, `PxSize`, `PxPadding`, `PxColorRgba`, `PxColorHsl`), and references
no UI framework. `ViewRules` is internal; the four view projects see it through
`InternalsVisibleTo` (P7.21).

## Creating an inspector

| Entry point | Builds |
|---|---|
| `Inspector.Create<T>()` | A typed inspector with every member of `T` (`TypeBinderMode.Automatic`) |
| `Inspector.Create<T>(TypeBinderMode.Manual)` | A typed inspector with no members; they come through `Add` (P1.12) |
| `Inspector.Create()` | An inspector with no type: members are added by name and found at the first bind (P1.14) |

A typed `Create` runs three steps: discovery builds the tree; the static
`DiscoveryFinished` is raised; then every member node gets the reflection
layer and the attribute layer, in that order. The static `Created` follows.
A failure in any step falls back and is reported (see Errors); only a type
that cannot be read at all is fatal, and the exception reaches the caller
after `DiscoveryFailed` reports it. An undefined `TypeBinderMode` throws
`ArgumentOutOfRangeException`.

`Create` locks the global options (`GlobalOptions`) for as long as the
inspector lives; `Dispose` releases the lock.

## The tree

| Node | Stands for |
|---|---|
| `MemberNode` | A public field or property |
| `CollectionNode` | A member that is a collection (P5.2, P5.10) |
| `ItemNode` | The item chosen in a collection: always the row right below it, named `Item` |
| `TypeGroupNode` | The members one type declares, when a member is hidden with `new` (P5.9) |
| `ButtonNode` | A button added by hand, with an action (P1.6) |
| `DisplayNode` | A value shown by hand, with a getter (P1.6) |

- **Paths.** `inspector["Moo.MooX"]`, or chained: `inspector["Moo"]["MooX"]`.
  `Node<T>(x => x.Moo.MooX)` reaches the same node, checked by the compiler.
  An unknown path throws `KeyNotFoundException`; a selector that starts from
  a type the node does not hold throws `ArgumentException`.
- **Hidden members.** When a type hides a member with `new`, each of the two
  sits in the group of the type that declares it (`Base.Value`,
  `Derived.Value`); `Value` alone finds the most derived one, as in C#. The
  other members stay directly under their owner.
- **Collections.** A collection shows its content, not the members of its type
  (`Count`, `Capacity`). Its `Item` row reads the item chosen with
  `SelectedIndex` in every bound object, and the members of the item's type
  hang below `Item`: `Items.Item.MooX`.
- **What a view shows.** `Rows` walks the tree depth first, leaving out ignored
  and hidden nodes and entering only groups (`IsGroup`); `ShownChildren` gives
  one level. Siblings come by `Order`, and in declaration order on a tie.
  Enumerating the inspector gives the whole tree, ignored nodes and the
  insides of closed groups too.
- **Groups.** A node is a group when it has members and is `Expandable`.
  `Collapsed` closes it in the view; `Visible` and `ReadOnly` are read through
  the parents, so hiding or locking a node takes its branch along.
- **Adding by hand.** `Add(name)`, `AddButton(name, text, press)`,
  `AddDisplay(name, read)`, and `AddField(...)` (below) work on the inspector
  (the root) and on member nodes. A name already used under the same node throws `ArgumentException`.
  In a typed inspector, `Add` finds the member at once (an unknown name throws
  `ArgumentException`; a node that holds a simple type or a collection throws
  `InvalidOperationException`), applies the reflection and attribute layers,
  and shows the node even where those layers would hide it. A member added
  this way comes without its own members, which are added the same way, and
  adding a child opens its parent. In an inspector with no type, `Add` keeps
  only the name until a bind finds the member; the bind then chooses the
  node's editor from the member's type, unless one was set, and no reflection
  or attribute layer applies.
- **Fields with their own reader and writer.** `AddField(name, valueType,
  read, write)`, or `AddField<TOwner, TValue>(name, read, write)` with
  `TOwner` a class, adds a `FieldNode`: a `MemberNode` with no member behind
  it, whose reader and writer get the object the node sits in, in each bound
  object (the bound object itself under the root). It is read and written as a
  member is, with the options, the rules, the events, several bound objects,
  and mixed values; its editor follows `valueType` as reflection would choose
  it, and no attribute layer applies. With no writer it is read-only. What the
  reader or the writer throws is reported on the node, as a getter's or a
  setter's is. The object it sits in must be a class: writing into a struct
  throws `InvalidOperationException`. It is how a host shows fields that
  reflection cannot see, such as those a native component descriptor lists,
  with each bound object a handle to one native instance.

## Discovery

- The members are the public instance fields and properties, in the order
  they were declared, base types first (P5.1). An auto-property takes the
  place of its backing field; a computed property goes before the next
  auto-property, or last; an override stands where the property was first
  declared. When that order cannot be read, the reflection's order stays and
  the `Create` reports it.
- Indexers are left out. A member whose type cannot be read (an assembly it
  uses is missing) is left out and reported as `Critical`.
- Discovery opens the type of every member, except simple types (primitives,
  enums, `string`, `decimal`, `object`, `DateTime`, `DateTimeOffset`,
  `TimeSpan`, `Guid`, and their nullable forms) and collections (any
  `IEnumerable` but `string`). A type already open on the way down is not
  opened again, so a cycle ends.
- The list of members and every attribute read are kept per type, in a
  `TypeModel` held in a `ConditionalWeakTable`, so they are read once per type
  and a type of an unloadable assembly is not held (P5.6). The nodes stay each
  inspector's own. A read that failed is kept, and each `Create` reports it
  again.

## Options

Every node carries its options: `Label`, `Tooltip`, `Help`, `Order`,
`Ignored`, `Visible`, `VisibleWhen`, `ReadOnly`, `Editor`, `Range`, `Choices`,
`ScrubMultiplier`, `ScrubAxis`, `Unit`, `ArcSpread`, `Expandable`,
`Collapsed`, and the lists `TextRules` and `ValueRules`. They are set in three layers; each overwrites
what the one before set.

1. **Reflection** (`ReflectionPolicy`). The label is the member's name. The
   editor follows the type: an enum is `Choice`, `bool` is `Toggle`, a number
   is `Number`, a collection is `Selector` (only on a `CollectionNode`;
   elsewhere `Display`), `object` and any type with members are `Display`,
   anything else is `Text`. A `readonly` or `const` field, and a property
   with no public setter or an init-only one, is read-only, except a
   collection held by reference, whose items stay editable (P4.7). A property
   whose setter is not public is ignored. A node with members is expandable
   unless `GlobalOptions.RequireExpandableAttribute` is on or its type fits
   more than one editor (below); a collection always is.
2. **Attributes** (`AttributePolicy`), on the member:

   | Attribute | Sets |
   |---|---|
   | `[InspectorReadOnly]` | `ReadOnly`, and brings back a member the reflection ignored |
   | `[InspectorIgnore]` | `Ignored`; it has the last word over `[InspectorReadOnly]` |
   | `[InspectorLabel(text)]` | `Label` |
   | `[InspectorTooltip(text)]` | `Tooltip`, the short description |
   | `[InspectorHelp(text)]` | `Help`, the long description behind the `(?)` mark (P7.15) |
   | `[InspectorEditor(kind)]` | `Editor` |
   | `[InspectorRange(min, max)]`, `Step` | `Range`; on a collection, its item's |
   | `[InspectorScrub(multiplier)]`, `Axis` | `ScrubMultiplier` and `ScrubAxis`; on a collection, its item's |
   | `[InspectorUnit(suffix, factor)]`, `Offset` | `Unit`; on a collection, its item's |
   | `[InspectorArc(spread)]` | `Editor` (`Arc`) and `ArcSpread` |
   | `[InspectorOrder(order)]` | `Order` |
   | `[InspectorExpandable]`, `Collapsed` | `Expandable` and `Collapsed`, on the member or on its type, when there is something to open |

   A name registered with `GlobalOptions.Hide<T>(names)` counts as an
   `[InspectorIgnore]` on that member of `T` and of the types derived from it
   (P6.7); the manual layer can still bring the member back (`Ignored = false`).
   An attribute that cannot be read or applied is skipped and reported as
   `WorkedAround`; the node keeps what reflection set.
3. **Manual**: whatever the caller sets on a node after the `Create`.

A type that fits more than one editor (`System.Drawing.Color`, `PxColorRgba`,
`PxColorHsl`) stays a closed `Display` row until an editor is chosen, by
`[InspectorEditor]`, `[InspectorExpandable]`, or the manual layer, and the
`Create` reports it as `WorkedAround`; the node is not made read-only, so
expanding it into its fields works (P5.5, P5.8). `Add` reports the same.

Two option scopes sit beside the nodes:

- **`GlobalOptions`** (static): `RequireExpandableAttribute`, and
  `Hide<T>`/`Unhide<T>`. Changing them while any inspector is alive throws
  `InvalidOperationException` (P1.5, P4.4); `Hide<T>` with a name `T` does not
  have throws `ArgumentException`.
- **`InspectorOptions`** (`inspector.Options`): the `Culture` that reads typed
  text (null means the current culture; `CultureInUse` is public, so a view
  shows values in the same culture, P7.21), the `BinderControl`, and the
  layout options.

A real change of a node's option, or of the culture or a layout option, raises
the inspector's `OptionChanged` (P7.8), so a view built before it can show it.
Setting `Choices` makes the editor `Choice`; setting `VisibleWhen` reads the
rule at once.

## Binding

- `Bind(instance)` binds one object; binding over a bound one throws
  `InvalidOperationException` (the swap is `Rebind`). `AddBind(instances)`
  adds objects (multi-bind); `RemoveBind(instance)` takes one out, and taking
  the last one out is `Unbind()`. `Rebind(instances)` unbinds and binds again;
  it checks the new objects first, so a refused one leaves the bind as it was.
- Only an instance of the inspector's type, or of a derived type, can be
  bound (`ArgumentException`), and each object once. With no type, the first
  bind into nothing fixes the type from its first object and finds every
  member added by name; a name the object lacks throws before anything
  changes, and `Unbind()` lets the next bind choose again. In such an
  inspector, a member with no public setter is read-only.
- A struct at the root is the inspector's own boxed copy: `Instance` reads it
  back after edits.
- `Instance` is the first bound object, `Instances` all of them in the order
  they were bound. `BindRegistered`, `BindRemoved`, and `Unbound` report the
  changes.

## Reading and writing

- **Reading.** `GetValue()` reads the first bound object now; `GetValues()`
  reads one value per object. `ViewValue` and `ViewValues` are what the view
  holds: the pending value, or what each object held at the last read, without
  reading the objects. `IsMixed` is true when the held values differ. A getter
  that throws is reported on the node (`Recovered`), and the read gives null
  for that object.
- **Writing.** `SetValue(value)` writes one value to every bound object;
  `SetValues(values)` writes one per object, in bind order, as scrubbing does
  (P7.16), and throws `ArgumentException` when the count differs. A value is
  prepared before anything is written:

  1. A value that is not text must fit the member: of its type, or a number
     into a number or an enum. Anything else is the caller's mistake and
     throws `ArgumentException`.
  2. Text goes through `TextRules`, then is parsed in `CultureInUse` (the
     type's `IParsable<T>`, or its `TypeConverter`); empty text is null for a
     nullable member. A number is converted to the member's number type.
  3. `ValueRules` run, then the `Range` clamps a number to `[Min, Max]`. The
     core does not round to `Range.Step`; the views do.

  A value that does not prepare (text that does not parse, a number too big
  for its type, a rule that throws) is reported on the node as `Recovered`,
  and nothing is written, to any object. A setter that throws is reported as
  `WorkedAround`. A write into a struct is written back into its owner, up the
  chain. Writing into a group, a button, a display, the root, a read-only node,
  or a node whose owner is null throws `InvalidOperationException`.
- **A display unit.** `Unit` (`DisplayUnit`: `Suffix`, `Factor`, `Offset`)
  shows and takes a number in another unit than the member holds:
  shown = held × `Factor` + `Offset`, rounded to 12 significant digits so a
  value that went in and back shows as typed. `DisplayUnit.Degrees` is an
  angle held in radians and shown in degrees (`DisplayUnit.RadiansToDegrees`
  is its factor, for the attribute). With a unit, `ViewValue`, `ViewValues`,
  and `IsMixed` are in it, as a `double` (`ViewType` is `double`, or
  `double?` for a nullable member), and so are the values `SetValue` and
  `SetValues` take, text included, and the `Range`; the value goes back to
  the member's unit before the `ValueRules`. Scrubbing moves each object by
  the same delta in the unit shown. `GetValue` and `GetValues` read the member
  as it is. A unit on anything but a number does nothing.
- **An arc.** `Editor = Arc` with `ArcSpread` naming a sibling edits the node
  as the center of an arc and the sibling as its width, both held in radians,
  whatever unit shows them; `ArcSpreadNode` finds the sibling, or null.
  `[InspectorArc(nameof(Spread))]` sets both. The sibling keeps its own row
  unless the caller hides it (`Ignored`); ignored, it is still read. A view
  that draws no arc shows the center as a number.
- **The binder control.** `InspectorOptions.BinderControl` is a flags enum:
  `ViewToInstance` sends a write to the objects at once; without it, the value
  waits in the node (`HasPendingValue`) until `Apply()`. `InstanceToView` lets
  a change in the objects reach the view on its own; without it, only
  `Reload()` reads them again. `Automatic` (both) is the default, `Manual`
  neither.
- **Refreshing.** `Refresh()` reads every node again and raises `ValueChanged`
  where something changed; it does nothing without `InstanceToView`.
  `Reload()` drops the pending values and reads again, whatever the binder
  control. Objects that implement `INotifyPropertyChanged`, at the root, as a
  group's object, or as a collection, update their nodes when they notify,
  under `InstanceToView`.
- **Forced flows**, for when the normal flow failed or does not fit:
  `ForceApply()` writes what the view holds into every object;
  `ForceReload()` starts over from what the objects hold, dropping pending
  values and failures and re-enabling disabled branches; `ForceClear()` empties
  what the view shows (zero, false, empty text, or null) without touching the
  objects. Each raises its own event.
- **`ValueChanged`** reports a change on a node with its `ValueSource`:
  `Write`, `Pending`, `Instance` (the object notified), `Refresh`, `Reload`,
  `Force`, or `Selection` (another item chosen in a collection).
- **Visibility rules.** `VisibleWhen` decides, with `Visible`, whether a row
  shows (P6.2). It is read with the values, against every bound object (the
  row shows only when it holds for all), and kept, so the view does not read
  the objects; `VisibleChanged` reports a new answer. A rule that throws shows
  the row and is reported.

### A group's object replaced from outside

A group of a class type remembers the object it showed in each bound object.
When a read finds another object there, the group raises `ObjectReplaced`
(P3.3, P3.4). A subscriber that sets `Accepted` keeps the branch on the new
object; otherwise the branch is disabled: `IsCompromised` is true, reads and
writes through it throw `InvalidOperationException`, and `Rebind()` or
`ForceReload()` restores it. An object replaced through the inspector, or a
closed object (a `Display` row) replaced, is an edit, not a swap. The item row
of a collection follows whatever the chosen place holds.

## Collections

A `CollectionNode` lists the items of the first bound object's collection
(`Items`, from the last read) and chooses one with `SelectedIndex` (`-1` for
none; a place past the items throws `ArgumentOutOfRangeException`). The choice
follows the item chosen when the items change. Its `ItemType` is an array's
element, the `T` of its `IEnumerable<T>`, or `object`.

`AddItem()`, `RemoveItem(index)`, and `MoveItem(from, to)` are the list editor's
operations (`EditorKind.List`). They change every bound object's collection at
once, whatever the binder control, as a button does. A new item is made with the
item type's parameterless constructor, or is the type's empty value, and becomes
the item chosen; the choice follows a moved item, and a removed chosen item
passes the choice to the one that takes its place. An array cannot grow or
shrink; a read-only sequence (`IEnumerable<T>`, `IReadOnlyList<T>`) cannot take
an item, and its item row is read-only. What an operation cannot do throws
before any collection changes.

## Buttons and displays

`AddButton(name, text, press)` makes a row with an action; `Text` is an option
like the others. `Press()` runs the action; pressing a read-only button or one
in a disabled branch throws, and an action that throws is reported on the row.
`AddDisplay(name, read)` shows the getter's value, read like a member on
`Refresh()` and `Reload()`; writing to it throws.

## Errors and recovery

The inspector does not crash on an internal error (section 3.11). Discovery,
reflection, the bound objects' getters and setters, the rules, and the views
are weak spots: a failure there falls back and is reported.

| Severity | Meaning | What happens |
|---|---|---|
| `Recovered` | An automatic fallback, with no ambiguity | The result is right; reported |
| `WorkedAround` | A semi-automatic fallback | Nothing broke, but the result may not be right; a subscriber can fix it and set `Handled` |
| `Critical` | No fallback | The node that failed is left out; the rest goes on |
| `Fatal` | The inspector cannot go on | The exception reaches the caller |

- During a `Create`, failures go to the static `DiscoveryFailed`, with the
  inspector as sender, and stay in `inspector.Report` (`Failures`,
  `ErrorCount`, `UnhandledErrorCount`, `CriticalErrorCount`). A subscriber to
  `DiscoveryFailed` can change the node at once and mark the failure handled.
- After the `Create`, a node's failure goes to its `BindFailed` and stays in
  its `Failure` until a read or a write works again, so a view shows it on the
  row. `InspectorFailureEventArgs` carries the path, the severity, the
  exception, a message, a `Reason`, and a `Suggestion`.
- Wrong use of the API by the caller throws at once and changes nothing:
  binding twice or the wrong type, writing a group, a disabled branch, a value
  of the wrong type, an unknown path, changing a global option with an
  inspector alive (P0.2).
- An exception thrown by a subscriber of the inspector's events goes up to
  whoever raised the event.

## Layout

`inspector.Layout(width)` returns an `InspectorLayout`: the rows a view shows,
each a `LayoutRow` with its node, depth, and the rectangles of the row, its
label, its help mark, and its editor, plus the panel of a group with its own
rows (P7.5). It runs with no UI and does not read the objects. A collapsed
group keeps its rows in a panel with no height, so a view can build them once.
A list editor is as high as the items it listed at the last read, up to
`ListRows`, plus a line for its buttons.

| Option | Default | Meaning |
|---|---|---|
| `RowHeight` | 23 | Height of a row |
| `RowSpacing` | 2 | Space between rows, and between a group's row and its panel |
| `Indent` | 16 | How far a group's rows go in, per level |
| `LabelWidth` | 120 | Width of the top-level labels; deeper ones lose the indent, so the editors line up |
| `LabelSpacing` | 4 | Space between a label and its editor |
| `Padding` | 4 on every side | Space around all the rows |
| `EditorMaxWidth` | 200 | The widest an editor gets; the rest of a wider row is space before it (P7.14) |
| `MaxWidth` | none | The widest the rows get, padding included |
| `HelpWidth` | 16 | Width of the `(?)` column, kept on every row when any node has `Help` |
| `ListRows` | 5 | Item lines a list editor shows before it scrolls |

The lengths are in the view's units (pixels in WinForms, device-independent
units in WPF); a negative length or a non-number throws.

## Lifecycle and threading

- `Inspector` and every node are `IDisposable`. Disposing the inspector
  unbinds its objects, disposes the tree, releases its lock on the global
  options, and raises `Disposed`, so a view that still shows it lets it go
  (P7.13). Binding, refreshing, applying, and the forced flows throw
  `ObjectDisposedException` on a disposed inspector; disposing twice does
  nothing.
- The creation events (`DiscoveryFinished`, `DiscoveryFailed`, `Created`) are
  static, because they are raised before anyone can subscribe to the new
  inspector; a subscriber outlives the inspectors it watches.
- The inspector has no thread affinity and no locking of its own: use it from
  one thread, normally the UI thread of its view, and raise the objects'
  `PropertyChanged` on that thread. Only the global options' lock and the type
  model cache are safe across threads.

## What the views rely on

- `ViewRules.KindFor(node)` gives the editor a row makes: none for a group,
  except a collection's selector or list; a `Slider` with no range is a
  `Number`, and so is an `Arc` that is not a number or whose `ArcSpread`
  names no number sibling; `Auto` (an inspector with no type, before the
  bind) is `Text`.
- `ViewType` is the type of what the view holds and writes: a `double` for a
  number shown in a `Unit`, the `ValueType` otherwise. A view picks its number
  widget by it, and shows the unit's `Suffix` if it chooses to.
- `ViewRules.CanScrub(node)`: a number with a non-zero `ScrubMultiplier`,
  writable and not in a disabled branch (P7.17).
- `ViewRules.ShowsMixed(kind, node)`: only a row with a value of its own shows
  its objects as mixed (P7.19).
- The event arguments the views raise live in the core:
  `ControlCreatedEventArgs<TControl>` and `RowCreatedEventArgs<TControl>`,
  with `RowPart` (`Label`, `Help`, `Editor`, `Panel`), for the escape valves
  (P7.4).
- `PxDock` (in `Primitives/`) mirrors WinForms' `DockStyle`, with the same
  values (P8.2); its conversions live in the WinForms view. No option of the
  core uses it today.

## Non-goals and edge cases

- No paging, no service locator, and no compatibility with the 0.7.1a API.
- A `[Flags]` enum is a single-choice list of its defined values
  ([PXI-PROPOSAL-007](../../proposals/flags-enum-editor.md)).
- `ValueChangedEventArgs` carries no previous value
  ([PXI-PROPOSAL-008](../../proposals/previous-value-in-value-changed.md)).
- A proxy property that returns a new class object on every read looks like
  an outside replacement of its group, every time
  ([PXI-ARCHIVE-008](../../archive/editor-discussion-2026-10-12.md)).
- The PixieLib primitives the core uses are `double`.
- Typed text that is not an integer is refused by an `int` member ("7.6"); a
  number into an `int` member is converted, with the culture.

## Validation expectations

No test project exists. A change to the core is checked by the
[development guide](../../development.md#checks): the solution builds with no
warning, the TuxHost output and the TerminalHost `--dump` check match their
saved output, and, for discovery or options, a parity console compares every
demo type's tree, options, and report before and after.
