# Core

**Document ID:** CORE-INTRODUCTION

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** introduction to the core module

**Surface:** introduction

**Boundary:** core

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

The core (`PixieInspector/`) turns a .NET type into a tree of nodes, one per
public field or property, binds objects to it, and reads and writes their
members. It knows no UI framework: a view shows the tree and calls the core to
edit it, and a host can use the core with no view at all.

```csharp
using var inspector = Inspector.Create<Foo>();
inspector["Moo.MooX"].Label = "Moo X";
inspector.Bind(new Foo());
inspector["Moo.MooX"].SetValue(42);
```

Start with the [technical reference](REFERENCE.md). Its most important
boundary: the inspector does not crash its host on an internal error; a weak
spot falls back and reports, and only wrong use of the API throws. The
[manifest](MANIFEST.md) routes the rest.
