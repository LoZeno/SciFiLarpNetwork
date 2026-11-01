# SPEC-01: Repository and layout

## Requirement

All new product code and project documentation belong in the `SciFiLarpNetwork` repository. The `Holonet3` repository is read-only source material for domain and behavior discovery; do not make migration changes there.

## Acceptance criteria

- A new solution is present at the `SciFiLarpNetwork` repository root.
- Each application has an application-level directory containing its own `src/` and `tests/` directories: `Backend/`, `Frontend/`, and `Management/`. Frontend project configuration also resides in `Frontend/`.
- Database deployment assets remain under the repository-level `deployment/` directory.
- A repository-local layout document describes these locations and makes clear that component project files and framework targets are introduced by their later implementation steps.
- No new runtime or build dependency requires a path outside the repository checkout, except documented SDK/tool installations.
