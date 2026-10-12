# WPF View

**Document ID:** VIEW-WPF-INTRODUCTION

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** introduction to the view-wpf module

**Surface:** introduction

**Boundary:** view-wpf

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

The WPF view (`views/PixieInspector.Wpf/`) is the WinForms view on the other
platform: a `UserControl` that puts a row per node on the rectangles of the
core's layout step, a canvas per group, inside a scroll viewer.

```csharp
var view = inspector.CreateWpfView();
window.Content = view;
```

Its most important boundary: Windows only (`net10.0-windows`), and it also
references WinForms, for the system's color dialog.

The [technical reference](REFERENCE.md) holds the contract, and the
[manifest](MANIFEST.md) routes the rest.
