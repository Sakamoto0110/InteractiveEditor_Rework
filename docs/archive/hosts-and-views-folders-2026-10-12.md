# Hosts and Views in Their Own Folders

**Document ID:** ARCHIVE-HOSTS-VIEWS-FOLDERS-20261012

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the hosts under hosts/ and the views under views/

**Surface:** archive-record

**Boundary:** global

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-006

**Type:** decision

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-12

**Commits:** `28a9bb30e8c97775ee61e509afaef59af41a767e`

**Current state:** [`README.md`](../../README.md#layout)

The owner asked for one folder for the hosts and one for the views. The seven
hosts moved to `hosts/`, with `DemoObjects`, which only the hosts use; the four
views moved to `views/`; the core stayed at the root, in `PixieInspector/`. The
folder names are lowercase, as in PixieEngine (`engine/`, `samples/`). The
solution groups the projects in matching solution folders, the project
references follow the new paths, and the `.gitignore` entry for the host
`NoHost` moved with it.

Verified after the commit with the same checks as
[PXI-ARCHIVE-005](rename-pixieinspector-2026-10-12.md): a clean build, the
TuxHost and TerminalHost outputs byte-identical, the parity console and the
unloadable-context test unchanged, and the ImGui harness at 25 of 25.
