# WinForms View

**Document ID:** VIEW-WINFORMS-INTRODUCTION

**Schema version:** 1

**Kind:** guide

**Lifecycle:** current

**Subject:** introduction to the view-winforms module

**Surface:** introduction

**Boundary:** view-winforms

**Authority role:** non-normative

**Mutation:** authored

**Indexing:** include

The WinForms view (`views/PixieInspector.WinForms/`) is a `UserControl` that
puts a row per node on the rectangles of the core's layout step, with a panel
per group. It is the view of the original inspector's platform.

```csharp
var view = inspector.CreateWinFormsView();
view.Dock = DockStyle.Fill;
form.Controls.Add(view);
```

Its most important boundary: Windows only (`net10.0-windows`); on Linux it runs
under Wine for verification, and the final look is checked on Windows.

The [technical reference](REFERENCE.md) holds the contract, and the
[manifest](MANIFEST.md) routes the rest.
