# Rename to PixieInspector

**Document ID:** ARCHIVE-RENAME-PIXIEINSPECTOR-20261012

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the rename of InteractiveEditor to PixieInspector: repository, solution, projects, namespaces, and assemblies

**Surface:** archive-record

**Boundary:** global

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-005

**Type:** decision

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-12

**Commits:** `733ca358467be4ea8c83abcc763a7b8fbbb52f9b`

**Current state:** [`README.md`](../../README.md), [`ROADMAP.md`](../../ROADMAP.md#active-guardrails)

The owner asked to rename the repository to PixieInspector and asked whether
the name was good. It joins the family (PixieLib, PixieEngine), says what the
project is (its central class is already `Inspector`), and removes an
ambiguity: PixieEngine described its editor as "built on InteractiveEditor", so
"editor" meant two different things.

The rename covered everything, not only the repository: the solution
(`InteractiveEditorSolution.slnx` became `PixieInspector.slnx`), the core and
the four view projects, their namespaces and assemblies, the project
references, `InternalsVisibleTo`, and the host window titles. `Inspector` and
the `[Inspector*]` attributes kept their names. Nothing had been published
under the old name, so no consumer broke; after a NuGet release, the same
change would have been a breaking one. The owner then renamed the GitHub
repository from `Sakamoto0110/InteractiveEditor_Rework` to
`Sakamoto0110/PixieInspector`, which redirects the old address.

The original 0.7.1a project, `Sakamoto0110/InteractiveEditor`, keeps its name;
the imported records still use the old names for this repository, as they did
at their baselines.

Verified after the commit: the solution built with 0 warnings and 0 errors; the
TuxHost output (67 lines) and the TerminalHost `--dump` output (7 targets, all
`ok`) were byte-identical to those before the rename; an out-of-repository
parity console (28 cases, each created three times) printed the same tree,
options, and report as before; an unloadable-context test still collected its
context; and the headless ImGui harness passed 25 of 25 scenarios. The
interactive hosts were built but not run.
