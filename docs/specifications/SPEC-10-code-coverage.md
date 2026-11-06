# SPEC-10: Minimum code coverage

## Requirement

Maintain at least 80% automated code coverage for hand-written production code. Report .NET and TypeScript coverage separately so a high-coverage subsystem cannot hide an uncovered one.

## Acceptance criteria

- CI produces machine-readable and human-readable coverage reports and fails when either production-code measurement falls below 80%.
- .NET line coverage is measured for hand-written production C#; TypeScript statement/line coverage is measured for hand-written production TypeScript.
- Only generated code and third-party code may be excluded, and exclusions are explicit and narrowly scoped.
- Coverage checks run with the relevant test suites and are reproducible using documented commands.

## Baseline implementation

- Backend Integration and Management xUnit.net v3 projects use Coverlet's MTP extension to measure `SciFiLarpNetwork.*` production assemblies, excluding test assemblies and compiler-generated code. They emit uniquely named Cobertura XML and JSON reports plus a readable console summary, and enforce 80% total line coverage. The .NET 10 SDK uses the MTP `dotnet test` runner selected in `global.json`.
- Vitest uses its V8 provider to measure all `Frontend/src/**/*.ts` and `Frontend/src/**/*.tsx` files, excluding declaration-only `.d.ts` files. It emits text, LCOV, and HTML reports and enforces 80% statements and lines.
- The CI workflow uploads coverage reports from each platform as artifacts.
- Backend production code, the management application, and the React SPA have active coverage thresholds. Run `dotnet test SciFiLarpNetwork.slnx --configuration Release` from the repository root for .NET tests and coverage; run `npm run check` from `Frontend` for TypeScript tests and coverage.
