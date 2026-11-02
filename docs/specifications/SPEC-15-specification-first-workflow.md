# SPEC-15: Specification-first workflow

## Requirement

Analyze the legacy repository, create and review these specification files in `SciFiLarpNetwork`, then wait for the user to select a specification before beginning implementation.

After implementation begins, use [SPEC-22](SPEC-22-implementation-order.md) as the persistent, cross-session execution and handoff record. An agent joining later must determine its work from the first unchecked roadmap row and repository state, not from assumptions about prior conversation history.

## Acceptance criteria

- During specification authoring, no application code, project files, database schema, generated source, or existing repository content is changed; only specification documents are added.
- Before each task, inspect the worktree and read the first unchecked SPEC-22 roadmap row, its specification, and `docs/architecture/domain-language-and-scope.md`. Limit the task to that specification and directly required dependencies; preserve the domain vocabulary and scope guide's explicit omissions, and call out any proposed scope expansion.
- Resolve material open decisions (including any constraint conflicts discovered during implementation) with the user before making irreversible or scope-changing choices.
- Mark a roadmap row complete only after its acceptance criteria are satisfied, appropriate checks are run, and its changes are persisted in the repository. Record the verification, remaining limitations, and next unchecked row in the handoff.
- Work proceeds incrementally, with tests and acceptance evidence for each approved implementation task.

## Workflow state

- The legacy analysis and specification set were created before implementation began.
- The user explicitly authorized implementation, which proceeds in SPEC-22 order.
- SPEC-22 records completed tasks 01 and 02; this specification records completion of task 03.
- Each task's changes remain in the repository worktree until committed or otherwise persisted by the user; handoffs must inspect, preserve, and report existing work rather than discard it.
