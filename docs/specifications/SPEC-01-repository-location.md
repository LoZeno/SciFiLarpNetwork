# SPEC-01: Repository and layout

## Requirement

All new product code and project documentation belong in the `SciFiLarpNetwork` repository. The `Holonet3` repository is read-only source material for domain and behavior discovery; do not make migration changes there.

## Acceptance criteria

- The new solution, application projects, tests, and build/development documentation are committed under `SciFiLarpNetwork`.
- Project layout clearly separates the .NET backend, React/TypeScript SPA, Avalonia management application, and automated tests.
- No new runtime or build dependency requires a path outside the repository checkout, except documented SDK/tool installations.
