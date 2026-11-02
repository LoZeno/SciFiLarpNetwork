# SPEC-11: xUnit testing

## Requirement

Use xUnit for .NET unit, integration, and backend contract tests. Keep tests automated, deterministic, and runnable through the .NET CLI and CI.

## Acceptance criteria

- All .NET test projects use xUnit and target a supported .NET 10 test framework.
- Tests cover domain decisions, event persistence/projections, MVC endpoints, and cross-client API contracts at appropriate levels.
- Integration tests use isolated test data and do not depend on a developer's local database or machine-specific paths.
- TypeScript code is type-checked and tested with a TypeScript-compatible test runner; .NET-only xUnit tooling is not used to avoid testing the frontend.
- Test and coverage commands are documented and run in CI.

## Baseline implementation

- `Backend/tests/SciFiLarpNetwork.Backend.Tests` and `Management/tests/SciFiLarpNetwork.Management.Tests` are SDK-style xUnit projects targeting `net10.0`.
- `docs/architecture/testing-strategy.md` documents commands, deterministic test design, and isolated integration-test data/service requirements.
- `.github/workflows/ci.yml` runs the root .NET solution test command and the frontend's documented checks on pull requests and pushes to `main`.
- No no-op placeholder tests are counted as coverage. Behavioral tests are added with the relevant application/domain work; coverage thresholds are established by SPEC-10.
