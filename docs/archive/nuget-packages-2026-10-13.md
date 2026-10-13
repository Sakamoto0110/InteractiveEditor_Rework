# The Packages on nuget.org

**Document ID:** ARCHIVE-NUGET-PACKAGES-20261013

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** publishing the core and the four views to nuget.org as five packages at one version, under the MIT license

**Surface:** archive-record

**Boundary:** global

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-011

**Type:** decision

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-13

**Commits:** `d47548826ed29c320176a968fb3dbc9ecea16e66`, `ab9c47c5a029e8fb7dc81340f6e77a284ec98d90`, `8b471dc84f821fcc10848b8ccf8d657e56a5700c`

**Current state:** [`ROADMAP.md`](../../ROADMAP.md#active-guardrails), [`docs/development.md`](../development.md#packages)

PixieEngine's editor needs a public, pinned version of the inspector: its
documentation must be able to say which PixieInspector it runs. Until then
nothing had been published, and the only stable reference was a commit.

## Decisions

- **Publish the way PixieLib does.** A workflow started by hand from `main`
  runs CI, stops if the tag `v<Version>` exists, pushes to nuget.org by trusted
  publishing (the policy is on the owner's account, Nekovih), and makes a
  GitHub release with that tag. No API key lives in the repository.
- **Every view is a package**, not only the ImGui view the engine's editor
  uses: the owner wants all of them available, just in case.
- **One version for all five**, in `Directory.Build.props`, raised together
  even when only one view changed, so a consumer pins one number.
- **A view requires the core of its own version exactly** (`[0.1.0]`). The
  views see the core's internal `ViewRules` (P7.21), which may change in any
  release; with the default range (`>= 0.1.0`), NuGet could put a view next to
  a newer core, and the mismatch would surface as a `MissingMethodException`
  at run time. `dotnet pack` turns a project reference into a `>=` range, so
  `Directory.Build.targets` rewrites it.
- **MIT**, as PixieLib. The repository had no license, which would have made
  the packages all rights reserved.
- **First version 0.1.0**, on PixieLib 0.2.0.

The publish workflow adds `--skip-duplicate` to PixieLib's push, so that a run
that fails after pushing some of the five packages can simply be run again.

## Verification

On Windows, .NET SDK 10.0.401, at `8b471dc`:

- `dotnet pack PixieInspector.slnx` produced five `.nupkg` and five
  `.snupkg`, and nothing for the hosts or `DemoObjects`.
- The `.nuspec` files: the core depends on `PixieLib` 0.2.0; each view on
  `PixieInspector` `[0.1.0]`, plus `Hexa.NET.ImGui` 2.2.9 (ImGui) or `Avalonia`
  12.1.3 (Avalonia); WinForms and WPF target `net10.0-windows7.0` with their
  framework references. Each carries the MIT expression, the root README, and
  the repository commit.
- A scratch console outside the repository, with its own `NUGET_PACKAGES`,
  restored `PixieInspector.ImGui` 0.1.0 from a local feed, got the core 0.1.0
  and PixieLib 0.2.0 with it, created an inspector and an ImGui view, and ran.
  Adding `PixieInspector` 0.1.1 next to it gave `NU1608` (a warning: NuGet
  does not let a package make it an error).
- The CI steps ran locally: the build in Release with warnings as errors,
  0 warnings; the TuxHost, 67 lines; `TerminalHost --dump`, exit code 0; the
  pack after the build.

Not verified here: the workflows themselves, which run only on GitHub, and the
pack on Linux, which the CI's Linux job covers.
