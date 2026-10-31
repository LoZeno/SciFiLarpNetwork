# SPEC-01: Repository and layout

## Requirement

All new product code and project documentation belong in the `SciFiLarpNetwork` repository. The `Holonet3` repository is read-only source material for domain and behavior discovery; do not make migration changes there.

## Acceptance criteria

- A new solution is present at the `SciFiLarpNetwork` repository root.
- Repository-local directories clearly reserve separate locations for the .NET backend, React/TypeScript SPA, Avalonia management application, their tests, and database deployment assets.
- A repository-local layout document describes these locations and makes clear that component project files and framework targets are introduced by their later implementation steps.
- No new runtime or build dependency requires a path outside the repository checkout, except documented SDK/tool installations.
