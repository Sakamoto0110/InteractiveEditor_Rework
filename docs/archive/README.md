# Archive Router

**Document ID:** GLOBAL-ARCHIVE-INDEX

**Schema version:** 1

**Kind:** reference

**Lifecycle:** current

**Subject:** router to the archive records, one row per record

**Surface:** index

**Boundary:** global

**Authority role:** non-normative

**Mutation:** append-only

**Indexing:** include

One row per archive record, in `Archive ID` order. A row says enough to decide
whether a record is the one you need; the record holds the full context. The
rules are in the [documentation policy](../governance/documentation-policy.md#archive).

Records PXI-ARCHIVE-001 to 004 are the documents written before the policy,
kept whole and in Portuguese. The source cites the modernization notes by
question number (`P5.6`) and by section (`3.11`): look those up in
PXI-ARCHIVE-001.

| ID | Date | Type | Origin | Author | Summary | Commits | Record |
|---|---|---|---|---|---|---|---|
| PXI-ARCHIVE-001 | 2026-10-10 | import | owner decision | claude | Modernization notes (pt-BR): the 0.7.1a analysis, every decision P1.1 onward with its commits, the designs, the checklist | `997c70d` | [`modernization-notes-2026-10-10.md`](modernization-notes-2026-10-10.md) |
| PXI-ARCHIVE-002 | 2026-10-10 | import | owner decision | claude | Context handoff (pt-BR): commit rules, working agreements, how to verify, the code and the next steps at the time | `997c70d` | [`context-handoff-2026-10-10.md`](context-handoff-2026-10-10.md) |
| PXI-ARCHIVE-003 | 2026-10-09 | import | owner decision | claude | PixieLib session handoff (pt-BR), from before PixieLib moved to its own repository and to nuget.org | `997c70d` | [`pixielib-handoff-2026-10-09.md`](pixielib-handoff-2026-10-09.md) |
| PXI-ARCHIVE-004 | 2026-10-09 | import | owner decision | claude | Open-questions file (pt-BR), empty at the time; the answer-by-number convention | `997c70d` | [`open-questions-2026-10-09.md`](open-questions-2026-10-09.md) |
| PXI-ARCHIVE-005 | 2026-10-12 | decision | owner decision | claude | Rename to PixieInspector: repository, solution, projects, namespaces, assemblies | `733ca35` | [`rename-pixieinspector-2026-10-12.md`](rename-pixieinspector-2026-10-12.md) |
| PXI-ARCHIVE-006 | 2026-10-12 | decision | owner decision | claude | The hosts under `hosts/`, the views under `views/`, the core at the root | `28a9bb3` | [`hosts-and-views-folders-2026-10-12.md`](hosts-and-views-folders-2026-10-12.md) |
| PXI-ARCHIVE-007 | 2026-10-12 | completion | PXI-TODO-001 | claude | The documentation policy adopted: derived from PixieEngine and VAW, five modules, the pre-policy documents archived whole | `997c70d`, `985e597`, `390c103`, `c565afd`, `cbf234d`, and the record's own | [`documentation-foundation-2026-10-12.md`](documentation-foundation-2026-10-12.md) |
| PXI-ARCHIVE-008 | 2026-10-12 | proposal | owner discussion | claude | What PixieEngine's editor needs; eight proposals, none promoted | with the record | [`editor-discussion-2026-10-12.md`](editor-discussion-2026-10-12.md) |
