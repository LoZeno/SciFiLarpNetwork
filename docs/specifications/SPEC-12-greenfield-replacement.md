# SPEC-12: Greenfield replacement

## Requirement

The existing contents of `SciFiLarpNetwork` may be replaced as part of implementation. The new solution is a clean, maintainable .NET 10 / React / Avalonia design informed by the legacy domain, not a requirement to preserve the existing .NET Framework, Entity Framework, SQLite, or NUnit structure.

## Implementation decision

The legacy `SciFiNetwork` subtree was removed for the greenfield replacement. This deliberately removes its .NET Framework project files, Entity Framework model and generated code, SQLite schemas and sample databases, native binaries, vendored NuGet packages, and old solution metadata. The repository root's new `SciFiLarpNetwork.slnx`, source/test/deployment directory scaffold, documentation, and Git metadata/history are retained. The separate `Holonet3` source repository remains untouched.

No legacy data import, schema compatibility, or backward-compatibility behavior is included by this decision. Any such capability requires a separately approved specification.

## Acceptance criteria

- Existing project code, schemas, binaries, sample databases, and project files are retained or removed deliberately as the approved implementation proceeds; do not delete or rewrite anything during the specification phase. The approved removal decision is recorded above.
- Preserve repository metadata and history. Do not rewrite Git history.
- Retain useful domain behavior only after it has been identified and restated in generic product terms.
- Any legacy data import, migration utility, or backward-compatibility commitment is treated as an explicit feature decision rather than assumed scope.
