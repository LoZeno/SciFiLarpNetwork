# SPEC-10: Minimum code coverage

## Requirement

Maintain at least 80% automated code coverage for hand-written production code. Report .NET and TypeScript coverage separately so a high-coverage subsystem cannot hide an uncovered one.

## Acceptance criteria

- CI produces machine-readable and human-readable coverage reports and fails when either production-code measurement falls below 80%.
- .NET line coverage is measured for hand-written production C#; TypeScript statement/line coverage is measured for hand-written production TypeScript.
- Only generated code and third-party code may be excluded, and exclusions are explicit and narrowly scoped.
- Coverage checks run with the relevant test suites and are reproducible using documented commands.
