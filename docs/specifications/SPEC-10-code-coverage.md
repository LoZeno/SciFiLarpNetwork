# SPEC-10: Minimum code coverage

## Requirement

Maintain at least 80% automated code coverage for hand-written production code. Report .NET and TypeScript coverage separately so a high-coverage subsystem cannot hide an uncovered one.

## Acceptance criteria

- CI produces machine-readable and human-readable coverage reports and fails when either production-code measurement falls below 80%.
- .NET line coverage is measured for hand-written production C#; TypeScript statement/line coverage is measured for hand-written production TypeScript.
- Only generated code and third-party code may be excluded, and exclusions are explicit and narrowly scoped.
- Coverage checks run with the relevant test suites and are reproducible using documented commands.

## Baseline implementation

- Backend and management xUnit projects use Coverlet MSBuild to measure `SciFiLarpNetwork.*` production assemblies, excluding test assemblies and compiler-generated code. They emit Cobertura XML and JSON reports plus a readable console summary, and enforce 80% total line coverage when the corresponding production project is present.
- Vitest uses its V8 provider to measure all `Frontend/src/**/*.ts` and `Frontend/src/**/*.tsx` files, excluding declaration-only `.d.ts` files. It emits text, LCOV, and HTML reports and enforces 80% statements and lines.
- The CI workflow uploads coverage reports from each platform as artifacts.
- Production source trees are currently empty. No-op tests or artificial source code are not added to make the threshold appear satisfied; until implementation begins, .NET produces empty reports without a zero-code threshold and the frontend reports its empty source include set. The thresholds activate as production projects and source are introduced.
