# SPEC-12: Greenfield replacement

## Requirement

The existing contents of `SciFiLarpNetwork` may be replaced as part of implementation. The new solution is a clean, maintainable .NET 10 / React / Avalonia design informed by the legacy domain, not a requirement to preserve the existing .NET Framework, Entity Framework, SQLite, or NUnit structure.

## Acceptance criteria

- Existing project code, schemas, binaries, sample databases, and project files are retained or removed deliberately as the approved implementation proceeds; do not delete or rewrite anything during the specification phase.
- Preserve repository metadata and history. Do not rewrite Git history.
- Retain useful domain behavior only after it has been identified and restated in generic product terms.
- Any legacy data import, migration utility, or backward-compatibility commitment is treated as an explicit feature decision rather than assumed scope.
