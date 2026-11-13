# SPEC-10: Minimum code coverage

## Requirement

CI enforces two separate coverage gates on hand-written production code:

| Gate | Code measured | Minimum |
|---|---|---|
| C# | All hand-written production C#, measured by all .NET test projects together | 80% line coverage |
| TypeScript | All hand-written production TypeScript in the frontend | 80% statement and 80% line coverage |

The gates are separate so that well-covered code in one language cannot hide poorly covered code in the other. CI fails when either gate is not met.

## Acceptance criteria

- CI produces machine-readable and human-readable coverage reports for both languages and fails when either gate is below 80%.
- The C# gate applies to the combined coverage of every .NET test project (unit and integration together), not to each test project on its own.
- Only generated code and third-party code may be excluded, and exclusions are explicit and narrowly scoped.
- Both gates can be reproduced locally with the documented commands.

## Implementation

### C# gate

- Every .NET test project under `Backend/tests` and `Management/tests` collects coverage with Coverlet's MTP extension (`coverlet.MTP`) and writes Cobertura XML and JSON reports to its own `TestResults/coverage/` directory. Coverage covers the `SciFiLarpNetwork.*` production assemblies and excludes test assemblies and compiler-generated code.
- Individual test projects have no threshold.
- After the tests, ReportGenerator (pinned as a local .NET tool in `dotnet-tools.json`) merges all the reports into one and fails when the combined line coverage is below 80%. Coverlet's MTP extension cannot merge reports itself.

### TypeScript gate

- Vitest's V8 provider measures every `Frontend/src/**/*.ts` and `Frontend/src/**/*.tsx` file, excluding declaration-only `.d.ts` files, writes text, LCOV, and HTML reports to `Frontend/coverage/`, and fails when statement or line coverage is below 80%.

### CI

- The `.NET tests` job runs the .NET tests, then the ReportGenerator merge and gate, adds the combined summary to the job summary, and uploads the per-project and combined reports as the `dotnet-coverage` artifact.
- The `Frontend checks` job runs `npm run check`, which includes the TypeScript gate, and uploads `Frontend/coverage/` as the `frontend-coverage` artifact.

The local commands for both gates are in [testing strategy](../architecture/testing-strategy.md#coverage-gates).
