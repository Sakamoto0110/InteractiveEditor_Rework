# A Branch per Task

**Document ID:** ARCHIVE-BRANCH-PER-TASK-20261013

**Schema version:** 1

**Kind:** roadmap/status

**Lifecycle:** historical

**Subject:** the end of the long-lived `rework-claude` branch, and a branch per task in its place

**Surface:** archive-record

**Boundary:** global

**Authority role:** non-normative

**Mutation:** snapshot

**Indexing:** include

**Archive ID:** PXI-ARCHIVE-012

**Type:** decision

**Origin:** owner decision

**Author:** claude

**Reference date:** 2026-10-13

**Commits:** with this record (parent `8b471dc84f821fcc10848b8ccf8d657e56a5700c`)

**Reference commit:** `940020dbd11aecf758ef4751099575db43b62a96`, `main` when the branches were deleted

**Current state:** [`docs/governance/documentation-policy.md`](../governance/documentation-policy.md#commits)

The commit rules sent all work to `rework-claude`, merged into `main` by the
owner. The branch was reused for pull requests #3 and #4, and two branches of
earlier pull requests, `claude/laughing-heisenberg-gx3vov` (#1) and
`claude/vibrant-fermi-smwjw5` (#2), were still on GitHub after their merges.

On 2026-10-13 the owner merged #4 and had every branch but `main` deleted,
locally and on GitHub: `rework-claude` was redundant once merged and had been
causing confusion. Each deleted branch was checked first to be contained in
`main`. The local copy of `claude/vibrant-fermi-smwjw5` held one commit,
`36068a7`, that is not an ancestor of `main`, but `git cherry` and
`git range-diff` showed its patch identical to `db14e81`, already there.

From then on, work goes to a branch of its own per task and reaches `main`
through a pull request merged by owner decision; a merged branch is deleted.
