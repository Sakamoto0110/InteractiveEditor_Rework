# Documentation Policy

**Document ID:** GLOBAL-DOCUMENTATION-POLICY

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** authority, lifecycle, ownership, scheduling, and archiving rules for PixieInspector documentation

**Surface:** policy

**Boundary:** global

**Authority role:** normative

**Mutation:** authored

**Indexing:** include

This policy defines where each kind of PixieInspector fact is owned and how
documents move between states. The structural vocabulary lives in the
[documentation schema](../schemas/documentation-schema.json); this policy owns
the meaning of that vocabulary.

The model is derived from the PixieEngine documentation policy and schema
(schema version 1, observed 2026-10-12 at PixieEngine `46ee6aa`), which is in
turn a partial derivation of the NekoLib model that Void Agent Workplace
extracted. The derivation and every intentional difference are recorded in the
[schema index](../schemas/README.md). Neither project is an authority over this
repository.

## Authority

Every fact has exactly one owner. Other documents may summarize a fact and link
to its owner; they must not maintain a competing mutable copy.

| Fact | Owner |
|---|---|
| Implemented behavior | Current source code |
| Targets, references, package references, and build properties | Each project file (`*.csproj`) |
| Solution membership | `PixieInspector.slnx` |
| Dependencies (toolchain, packages, own projects), versions, status, and candidates | Root `DEPENDENCIES.md` |
| Public API | The compiled assemblies of the core and the views; no accepted API baseline exists yet |
| Module technical contract | `docs/modules/<boundary>/REFERENCE.md` |
| Module routing and identity | `docs/modules/<boundary>/MANIFEST.md` |
| What PixieInspector is and is not, direction, guardrails, and freezes | Root `ROADMAP.md` |
| Formally promoted work, ordering, gates, and completion criteria | Root `TODO.md` |
| Decomposition of a promoted `TODO.md` entry into tasks | Root `TASKLIST.md` |
| Unpromoted ideas | One record per file under `docs/proposals/` |
| How to build, check, and commit a change | `docs/development.md`, under the commit rules below |
| What a host shows and how to run it | `hosts/<host>/README.md` |
| Confirmed defects | Module `ISSUES.md` |
| Unconfirmed observations | Module `FINDINGS.md` |
| Evidence requirements | Module `VALIDATION_REQUIREMENTS.md` |
| Executed evidence | Module `VALIDATIONS.md`, tests, and scenarios |
| Chronology, completed work, superseded state, and dated rationale | `docs/archive/` |
| Documentation structure and vocabulary | `docs/schemas/documentation-schema.json` |
| Open questions and answers before promotion | `.local/` (unversioned; never authority) |

Generated output is never authority and is never versioned: `bin/`, `obj/`, and
the `imgui.ini` a host writes where it runs.

## Scheduling: `TODO.md` and `TASKLIST.md`

`TODO.md` is the only scheduler. An entry enters it only through an explicit
owner decision with its scope, gates, and completion criteria formalized. A
change to the scope of a promoted entry also needs an owner decision and is
recorded in the entry with its date.

When an entry completes, it moves to `docs/archive/` as a snapshot, together
with its `TASKLIST.md` section if it has one, its verification, and the commits
that delivered it; `TODO.md` keeps a one-line pointer to the archive record.

`TASKLIST.md` is a child scheduler of `TODO.md`. It exists so that an entry
whose scope is large, or that decomposes into many small tasks, does not
pollute `TODO.md`. Its rules:

- Every section belongs to exactly one `TODO.md` entry, named in its `Parent`
  field, and that entry links back to the section in its `Breakdown` field.
  A task without an existing parent is invalid.
- Tasks decompose the parent's accepted scope; they never extend it. Work
  outside that scope is promoted to `TODO.md` first.
- The parent completes only when each of its tasks is `done` or `dropped`, and
  every `dropped` task states why.
- When the parent completes, its section moves to `docs/archive/` with the
  parent's record and leaves `TASKLIST.md`.
- Task IDs are global and stable (`PXI-TASK-001`); they are not renumbered
  when a parent changes, completes, or is archived.

`ROADMAP.md` states direction but does not authorize implementation. A
proposal, finding, issue, archive record, or direct owner decision becomes work
only through formal promotion to `TODO.md`.

## Dependencies

`DEPENDENCIES.md` lists every dependency: toolchain, packages, and the owner's
own projects. Each one has a status observed on the file's `Last checked`
date:

- `ok`: present where it is consumed, at a current or deliberately pinned
  version;
- `outdated`: present, but a newer release exists and the gap matters;
- `not-installed`: not present yet.

A suggested dependency stays under candidates until the owner adopts it. A
status is an observation from its date, not a guarantee; recheck before a
version change. The project files own the wiring: which project references
which package, at which version. `DEPENDENCIES.md` records why and the status.

## Unversioned working documentation: `.local/`

`.local/` holds working documents that are deliberately not versioned and meant
for quick use, such as `.local/decisions/open-questions.md`. Everything in it is
safe to delete at any time. Questions are numbered so the owner can answer by
number. After an answer, an item is either promoted, expanded into `TODO.md`,
recorded in `ROADMAP.md`, turned into a proposal, or discarded.

`.local/` is never authority, and versioned documents never link into it,
because a clone does not contain it. An answer that shapes code, structure, or
direction must be promoted to a versioned owner; an unpromoted answer is not a
repository fact.

## Archive

`docs/archive/` owns the history of the repository: what changed, what was
completed, and why things changed. Every archived event is one file, however
small: a decision, a promotion, a completed `TODO.md` entry with its
`TASKLIST.md` section, a fix, a finding, a proposal outcome, a dependency
change, or a roadmap change. Material imported for provenance, from another
repository or from this repository's documentation before this policy, is
archived the same way.

Every record identifies what produced it, when, and in which commits:

- `Archive ID`: a stable `PXI-ARCHIVE-NNN`, assigned in order and never reused.
- `Type`: `decision`, `promotion`, `completion`, `fix`, `finding`, `proposal`,
  `dependency`, `roadmap`, or `import`.
- `Origin`: the identity of what produced the event, such as `PXI-TODO-001`,
  `PXI-PROPOSAL-002`, `CORE-FINDING-001`, or `owner decision`.
- `Author`: who wrote the record: `owner`, `claude`, or `codex`. It names the
  writer of the record, not of the commits, which carry the owner's identity.
- `Reference date`: when the event happened.
- `Commits`: the full hashes of the commits that carried the event. A record
  added in the same documentation-only commit as its event cannot name that
  commit; it says `with this record (parent <full hash>)`, and
  `git log -S <Archive ID>` finds it.
- `Reference commit`, optional: the baseline a record describes, when it
  differs from `Commits`.
- `Original path`, for a moved or imported record; imported records name the
  source repository in it.

A record about one module carries that module's key in `Boundary`; there is no
second, per-module history.

[`docs/archive/README.md`](../archive/README.md) is a router: one table row per
record, in `Archive ID` order, with the ID, date, type, origin, author, a
one-line summary, short commit hashes, and the link to the record. A row says
enough to decide whether a record is the one needed; the record holds the full
context. Find the event in the router, then read only its record.

- Records are `Lifecycle: historical` and `Mutation: snapshot`. The body and
  baseline are preserved; only narrowly identified metadata or link
  corrections and appended reconciliation sections are allowed.
- A record is true only for its baseline. It never owns current technical
  facts, open work, or direction.
- An imported record keeps the language of its body. The documents written
  before this policy (PXI-ARCHIVE-001 to PXI-ARCHIVE-004) are in Portuguese,
  under an English header, and the source still cites them by question number
  (`P5.6`) and by section (`3.11`).

## Module surfaces

A module is one boundary under `docs/modules/<boundary>/`: the core, or one
view. A module is created when its boundary is decided.

Required from creation:

- `MANIFEST.md` routes identity, projects, target frameworks, dependencies,
  public API surface, documentation surfaces, evidence locations, and related
  boundaries. It does not reproduce public symbols.
- `README.md` is a concise introduction.
- `REFERENCE.md` is the single normative current technical contract.

Created on demand, together with their first record: `ISSUES.md`,
`FINDINGS.md`, `VALIDATION_REQUIREMENTS.md`, `VALIDATIONS.md`, `CHANGELOG.md`,
`INTERNALS.md`, `audits/`, and `migrations/`. The absence of an on-demand
surface means no record has been written; it is not evidence that no defect,
finding, or requirement exists.

`Related boundaries` aids traversal only. It does not create or imply a build
or package dependency.

## Hosts, scenarios, and oracles

The hosts under `hosts/` are samples: each demonstrates what the core and a
view already do. A host is documented by one `README.md` in its own directory:
what it shows, how to build and run it, and its options. It is not a module.
`hosts/DemoObjects/` holds the objects the hosts inspect and is documented the
same way.

A project with a purpose beyond demonstration needs more: a scenario proves a
specific claim, and an oracle breaks something on purpose to show that the
failure is caught. Its `README.md` also states the claim or the failure, the
expected outcome, and how to read the result. What such a project demonstrates
is evidence; it never defines behavior.

## Commits

- Messages are in English and start with a prefix in parentheses: `(feat)`,
  `(fix)`, `(refactor)`, `(testing)`, `(chore)`, or `(docs)`. The subject is
  short; the body says what changed and why.
- Code and documentation go in separate commits. The documentation commit that
  records code work cites the hashes of the code commits.
- Commits carry the owner's identity only, as author and committer:
  `Rafael Sakamoto <rafael.sakamoto1@hotmail.com>`, with no co-author or tool
  attribution lines.
- Work is pushed to the `rework-claude` branch. `main` changes only by an owner
  decision, through a merge.
- History may be rewritten only before it is pushed; afterwards, a correction
  is a new commit.

## Metadata

Indexable documents carry the bold-label metadata defined by the schema, one
value per physical line, near the top of the file. `Kind`, `Lifecycle`,
`Surface`, `Boundary`, `Authority role`, `Mutation`, and `Indexing` are
independent dimensions. Paths in metadata are repository-relative with forward
slashes.

- `current` is maintained with the repository.
- `frozen` stays live but cannot expand without its recorded unfreeze condition.
- `historical` is a dated snapshot and never current-state authority.
- An empty registry states its baseline explicitly.

Document IDs and record IDs are stable and never renumbered or reused.

## Language and authoring

Versioned and `.local/` documentation is written in English, except the body of
an imported record. Documents must be understandable without a private prompt,
a conversation, an external workspace, or knowledge of which person or tool
wrote them. External procedures, including agent tooling, may add emphasis but
do not own a format, path, authority rule, or acceptance criterion here.

## Conflict handling

- If source and a current normative reference disagree, record the exact
  conflict and stop for disposition; do not silently rewrite either side.
- Evidence demonstrates claims but does not define behavior.
- A finding becomes an issue only after verification.
- Historical bodies are not rewritten to match later truth; later outcomes are
  recorded separately.

## Enforcement

No documentation verifier exists yet. Until one does, these rules are applied
by review only, and a document that satisfies them has not been mechanically
checked. Claims about structural validity must say so.
