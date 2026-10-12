# DemoObjects

The objects the hosts inspect. It references only the core, for the
attributes, and gets PixieLib through it.

| Type | Namespace | What it exercises |
|---|---|---|
| `Foo`, `Moo`, `Doo` | `DemoObjects.ClassObjects` | Plain classes nested in each other: groups, discovery, and the options layers |
| `Boo`, `Coo` | `DemoObjects.StructObjects` | Structs with a nested struct: an edit two levels down is written back through both copies |
| `Boo` | `DemoObjects.AttributedObjects` | Each of the ten `[Inspector*]` attributes |
| `Hoo` | `DemoObjects.HybridObjects` | Properties of every simple kind (text, toggle, enum, nullable) and a struct member |
| `Gadget`, `GadgetMode` | `DemoObjects.ViewObjects` | A member for each editor of the views, long help, and scrubbing in both directions |

Two types are called `Boo`; the hosts alias one of them.
