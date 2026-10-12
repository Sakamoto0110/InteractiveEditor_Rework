# PixieInspector Dependencies

**Document ID:** GLOBAL-DEPENDENCIES

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** every dependency, its version, status, and candidates

**Surface:** dependency-registry

**Boundary:** global

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

**Last checked:** 2026-10-12

This is the master list of what PixieInspector depends on: toolchain, packages,
and the owner's own projects. It owns which dependency is used, why, and its
current status. The project files own the wiring: which project references
which package, at which version.

## Status

| Status | Meaning |
|---|---|
| `ok` | Present where it is consumed, at a current or deliberately pinned version |
| `outdated` | Present, but a newer release exists and the gap matters |
| `not-installed` | Not present yet |

"Latest" is the newest stable release on nuget.org on the `Last checked` date.
Statuses are observations from that date, not guarantees. Recheck before a
version change.

## Toolchain

| Dependency | Used for | In use | Latest | Status | Notes |
|---|---|---|---|---|---|
| .NET SDK 10 | Everything; C# 14 | 10.0.401 | — | `ok` | Builds the Windows targets on any system with `EnableWindowsTargeting`. |
| .NET desktop runtime (WinForms, WPF) | The WinForms and WPF views and hosts | From the SDK (`UseWindowsForms`, `UseWPF`) | — | `ok` | Windows only; on Linux the hosts run under Wine (see the [development guide](docs/development.md#windowed-hosts-on-linux)). The WPF view also uses WinForms, for its color dialog. |

## Packages

| Dependency | Used for | In use | Latest | Status | Notes |
|---|---|---|---|---|---|
| Hexa.NET.ImGui | The ImGui view's binding | 2.2.9 (Dear ImGui 1.92.2b, docking branch), in `views/PixieInspector.ImGui` | 2.2.9; 3.1.0-experimental (newest) | `ok` | Adopted 2026-10-10, replacing ImGui.NET 1.91.6.1. The host must use the same binding and version. |
| Hexa.NET.ImGui.Backends and Hexa.NET.ImGui.Backends.GLFW | ImGui's own OpenGL3 and GLFW backends, in `hosts/ImGuiHost` | 1.0.18 | 1.0.18 | `ok` | The versions made for Hexa.NET.ImGui 2.2.9. |
| Hexa.NET.GLFW | The ImGui host's window | 1.0.5, in `hosts/ImGuiHost` | 1.0.5 | `ok` | Replaced Silk.NET on 2026-10-10. |
| Avalonia | The Avalonia view and host | 12.1.3, in `views/PixieInspector.Avalonia` and `hosts/AvaloniaHost` | 12.1.4 | `ok` | 12.1.4 is a patch release; the gap does not matter yet. The host also uses `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, and `Avalonia.Fonts.Inter` at the same version. |
| Terminal.Gui | The terminal host | 2.5.0, in `hosts/TerminalHost` | 2.5.0 | `ok` | |

## Own projects

| Dependency | Used for | In use | Latest | Status | Notes |
|---|---|---|---|---|---|
| PixieLib (.NET) | The primitives: points, sizes, rectangles, colors | 0.1.0 from nuget.org, in `PixieInspector/` only; the views and hosts get it through the core | 0.1.0 | `ok` | `Sakamoto0110/PixieLib`. A change to it is made there and returns as a new package version. |

## Candidates

| Candidate | For | Notes |
|---|---|---|
| Hexa.NET.ImGui 3.x | A newer Dear ImGui (1.92.9b) | Experimental releases only; not adopted. |
