# Avalonia View

**Document ID:** VIEW-AVALONIA-INTRODUCTION

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** introduction to the view-avalonia module

**Surface:** introduction

**Boundary:** view-avalonia

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

The Avalonia view (`views/PixieInspector.Avalonia/`) is a property grid: a
`UserControl` with a row per node, the label on the left and the editor on the
right, and an expander per group. It runs wherever Avalonia does.

```csharp
var view = inspector.CreateAvaloniaView();
window.Content = view;
```

Its most important boundary: Avalonia's own panels lay the rows out; the
core's layout step is not used.

The [technical reference](REFERENCE.md) holds the contract, and the
[manifest](MANIFEST.md) routes the rest.
