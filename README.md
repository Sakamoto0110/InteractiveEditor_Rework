# PixieInspector

A reflection-based object inspector for .NET: point it at a type, bind one or
more objects, and it builds a tree of the members, reads and writes them, and
reports what it could not do. The core knows no UI framework; a view per
framework shows the tree and edits it. PixieEngine's C# editor uses it to
inspect engine state.

Early stage. The core and each view are a package on nuget.org, all five at
one version; a consumer takes the view it shows the inspector with, and the
core and PixieLib come with it:

| Package | Target | What it is |
|---|---|---|
| `PixieInspector` | `net10.0` | The core |
| `PixieInspector.ImGui` | `net10.0` | The Dear ImGui view, on Hexa.NET.ImGui; the host must use the same binding and version |
| `PixieInspector.Avalonia` | `net10.0` | The Avalonia view |
| `PixieInspector.WinForms` | `net10.0-windows` | The WinForms view |
| `PixieInspector.Wpf` | `net10.0-windows` | The WPF view |

```bash
dotnet add package PixieInspector.ImGui
```

From this repository:

```bash
dotnet build PixieInspector.slnx -p:EnableWindowsTargeting=true
dotnet run --project hosts/TuxHost/TuxHost.csproj
dotnet run --project hosts/TerminalHost/TerminalHost.csproj -- --dump
```

## Layout

| Path | What it is |
|---|---|
| `PixieInspector/` | The core: the inspector, its nodes, options, binding, and reports (`net10.0`) |
| `views/PixieInspector.ImGui/` | The Dear ImGui view, on Hexa.NET.ImGui (`net10.0`) |
| `views/PixieInspector.Avalonia/` | The Avalonia view (`net10.0`) |
| `views/PixieInspector.WinForms/` | The WinForms view (`net10.0-windows`) |
| `views/PixieInspector.Wpf/` | The WPF view (`net10.0-windows`) |
| `hosts/` | Sample applications, one per view and two consoles, and the objects they inspect |
| `docs/` | Documentation |

A consumer takes three assemblies: the core, one view, and PixieLib, which
supplies the primitives.

## Documentation

- [`ROADMAP.md`](ROADMAP.md): what PixieInspector is and is not, and where it is going.
- [`TODO.md`](TODO.md): promoted work. [`TASKLIST.md`](TASKLIST.md): its task breakdowns.
- [`DEPENDENCIES.md`](DEPENDENCIES.md): every dependency, its status, and candidates.
- [`docs/`](docs/README.md): documentation index, the module references, the
  development guide, proposals, and the archive.

MIT license; see [`LICENSE`](LICENSE).
