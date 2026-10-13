# Documentation Schemas

**Document ID:** GLOBAL-DOCUMENTATION-SCHEMAS

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** structural documentation schema and its derivation from PixieEngine

**Surface:** index

**Boundary:** global

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

[`documentation-schema.json`](documentation-schema.json) is the structural
contract for document metadata, module manifests, the two schedulers, archive
records, stable record IDs, and validation vocabulary. The meaning of that
vocabulary is owned by the
[documentation policy](../governance/documentation-policy.md).

## Derivation

The schema is a fork of PixieEngine `docs/schemas/documentation-schema.json`
(`pixieengine.documentation/1`), observed on 2026-10-12 at PixieEngine
`46ee6aa`. That schema is itself a partial derivation of NekoLib's, the model
Void Agent Workplace extracted. PixieEngine is the nearest relative: the same
family, and the owner's latest decisions about the model. NekoLib supplies what
PixieEngine left out because it only fits a .NET library.

Like PixieEngine, this is a documentation-only derivation: the repository is not
bound to a Void Agent Workplace profile, and nothing here depends on its tooling
or on its validation freeze.

| Area | PixieEngine | PixieInspector | Reason |
|---|---|---|---|
| Module boundary keys | `^[a-z][a-z0-9]*(\.[a-z][a-z0-9-]*)*$` | A hyphen is allowed in the first segment | The view keys (`view-imgui`); owner decision, 2026-10-12 |
| Manifest fields | Projects, language, artifact, platforms, and the rest | The same, plus `Target frameworks` | The core and two views are `net10.0`, two views `net10.0-windows`; taken from NekoLib's manifest |
| Manifest language and artifact | `cpp` or `csharp`; libraries, executables | `csharp`; `managed-assembly`, `executable`, or `none` | A C# repository |
| Manifest distribution | `shipped-runtime`, `editor-only`, `tooling`, `unshipped` | `package`, `sample`, or `unshipped` | The core and the views are meant to ship as packages; the hosts are samples |
| Public API oracle | A module's public header | The compiled assembly, with no accepted baseline | NekoLib keeps accepted manifests under `eng/public-api/`; there is no API tooling here yet |
| Working agreements | Commit rules inside the policy | The policy keeps the commit rules; `docs/development.md` holds build and verification | NekoLib's `docs/development.md`; this repository's day-to-day practice |
| Samples | `<sample directory>/README.md` | `hosts/<host>/README.md` | The hosts are the samples |
| Commit prefixes | `(feat)`, `(fix)`, `(refactor)`, `(docs)` | Adds `(testing)` and `(chore)` | Already in use in this repository's history |
| Commit identity and branch | Owner identity; nothing pushed without an owner decision | Owner identity named; a branch per task, merged into `main` through a pull request by owner decision | This repository's practice since 2026-09; a branch per task since 2026-10-13 ([PXI-ARCHIVE-012](../archive/branch-per-task-2026-10-13.md)) |
| Vendored code | Its own commit | Not covered | No vendored code; dependencies come from NuGet |
| Imported records | Archived the same way | The body keeps its language under an English header | The pre-policy documents are in Portuguese and cited from source; owner decision, 2026-10-12 |
| Validation boundaries | Includes `ipc` | Without `ipc` | No inter-process code |
| Stable IDs | `PIXIE-` | `PXI-` | Owner decision, 2026-10-12 |

Everything else follows PixieEngine unchanged: the metadata vocabulary, the
archive with its router, `TODO.md` with its child `TASKLIST.md`, the root
`DEPENDENCIES.md`, `.local/`, and module surfaces created on demand.

No verifier checks this schema yet; see the policy's enforcement section.
