# Documentation Foundation

**Document ID:** ARCHIVE-DOCUMENTATION-FOUNDATION-20261012

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the adoption of the documentation policy derived from PixieEngine and Void Agent Workplace, completing PXI-TODO-001

**Surface:** archive-record

**Boundary:** global

**Authority role:** evidence

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-007

**Type:** completion

**Origin:** PXI-TODO-001

**Author:** claude

**Reference date:** 2026-10-12

**Commits:** `997c70d68102a1c2a29f080a577f99f4c88054e3`, `985e597018405002ff1eb6c5b9f316477f79b01f`, `390c103d0f7efb89387598659a35af895933c40e`, `c565afdc4e6d379205caa5287a61d0d7312a0cec`, `cbf234d10dcf05b0978e3321192012d643df00d5`, and with this record (parent `cbf234d10dcf05b0978e3321192012d643df00d5`)

**Current state:** [`docs/governance/documentation-policy.md`](../governance/documentation-policy.md), [`docs/schemas/README.md`](../schemas/README.md)

## What happened

On 2026-10-12 the owner paused the work for PixieEngine's editor and asked for
three things: the rename to PixieInspector
([PXI-ARCHIVE-005](rename-pixieinspector-2026-10-12.md)), folders for the hosts
and the views ([PXI-ARCHIVE-006](hosts-and-views-folders-2026-10-12.md)), and
documentation normalized on PixieEngine's, which follows a partial derivation
of Void Agent Workplace (VAW). VAW is the extraction and decoupling of
NekoLib's documentation and agent infrastructure; it is frozen while it is
validated, and the freeze does not prevent a derivation. The owner granted
read access to it the same day.

The work followed the four steps of VAW's documentation-authoring contract
(`nekolib.documentation-authoring/1`), each authorized on its own:
`inventory/design` produced a migration map, which the owner accepted by
answering its six numbered questions (all yes); then `migrate-structure`,
`populate-module` for each of the five boundaries, and `validate-module`.

## Decisions taken with the map

1. The derivation follows PixieEngine's, the owner's latest decisions for the
   family, and takes from NekoLib only what a .NET library needs: the
   `Target frameworks` field and `docs/development.md`. Current documents are
   in English, replacing this repository's Portuguese rule. The schema index
   records every difference.
2. The four documents written before the policy are archived whole, in
   Portuguese, as imported records PXI-ARCHIVE-001 to 004, because the source
   cites them 355 times in 109 files by question and section number.
3. The boundaries are `core`, `view-imgui`, `view-avalonia`, `view-winforms`,
   and `view-wpf`; the hosts are samples.
4. Stable IDs use the prefix `PXI-`.
5. The eight ideas of the PixieEngine editor discussion are proposals, none
   promoted ([PXI-ARCHIVE-008](editor-discussion-2026-10-12.md)).
6. The owner's personal working preferences stay out of versioned documents;
   only the project's rules went to the development guide and the policy.

## The entry, as it closed

### `PXI-TODO-001` — Adopt the documentation policy

**Status:** completed

**Source:** owner decision on 2026-10-12, accepting the migration map of the
`inventory/design` step (questions 1 to 6)

**Accepted scope:** the documentation policy, schema, and templates derived
from PixieEngine's; the four documents written before it archived whole, in
Portuguese, as imported records; the root documents, the development guide,
and a `README.md` per host; the module documents (`MANIFEST.md`, `README.md`,
`REFERENCE.md`) of `core`, `view-imgui`, `view-avalonia`, `view-winforms`, and
`view-wpf`, written from the current source; the eight ideas of the PixieEngine
editor discussion as proposals. Out of scope: any change to product code or
public API, and the promotion of any proposal.

**Dependencies and gates:** the owner accepts the migration map; met on
2026-10-12.

**Breakdown:**

- `PXI-TASK-001` — **Status:** done — Archive the four documents written before
  the policy, whole, as PXI-ARCHIVE-001 to 004 (`997c70d`).
- `PXI-TASK-002` — **Status:** done — The policy, schema, templates, archive
  router, and the eight proposals (`985e597`).
- `PXI-TASK-003` — **Status:** done — The root documents, the development
  guide, and a `README.md` per host (`390c103`).
- `PXI-TASK-004` — **Status:** done — The module documents of `core`
  (`c565afd`).
- `PXI-TASK-005` — **Status:** done — The module documents of `view-imgui`
  (`cbf234d`).
- `PXI-TASK-006` — **Status:** done — The module documents of
  `view-avalonia`, `view-winforms`, and `view-wpf` (`cbf234d`).
- `PXI-TASK-007` — **Status:** done — The self-review of each module, then
  this record.

## Verification

- Each module reference was written from the source at `390c103` and checked
  claim by claim against it; where a first draft went beyond the source (for
  example, the label tooltip of the ImGui view, the failure color of the
  WinForms view, the italic label of mixed values), it was corrected before the
  commit.
- A review script checked every Markdown file against the schema: the required
  metadata and its vocabulary, the surface rules, the archive records' fields,
  unique document IDs, and every relative link and anchor, with the casing on
  disk. After this record: 57 files, 48 with metadata, no problem found. A
  copy with a broken anchor and a bad `Surface` value was caught, so the script
  detects both. It is a review aid, not the verifier the policy says does not
  exist yet.
- The bodies of PXI-ARCHIVE-001 to 004 were compared with the original files
  and are identical.
- No product code changed. The solution still builds with 0 warnings and 0
  errors, and the TuxHost and TerminalHost `--dump` outputs are unchanged.
- Not checked: the rendered look of the documents on GitHub, and links into
  other repositories.

## Observations

- The checklist in PXI-ARCHIVE-001 (section 6) keeps an open box for the
  error premise in the views (3.11). It is stale: section 0 and the summary of
  the same document record it applied in the views' fourth cut, commit
  `6632d73`. Its current owner is the `RowFailed` section of each view
  reference. The archived body is left as it was.
- No conflict between the source and a current reference was found that
  required a stop.
- Two questions for the owner remain from the handoff: checking the WindowsHost
  and the WpfHost on Windows (and the Avalonia, ImGui, and terminal hosts),
  and confirming the choices taken without asking in the views' fourth and
  fifth cuts. They are working questions, not repository facts.
