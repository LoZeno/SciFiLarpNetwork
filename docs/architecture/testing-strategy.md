# Testing strategy

## Test projects and commands

Backend and Avalonia management tests use xUnit with the .NET 10 test SDK. Test projects live with their application:

- `Backend/tests/SciFiLarpNetwork.Backend.Tests`
- `Management/tests/SciFiLarpNetwork.Management.Tests`

Run all .NET tests from the repository root:

```powershell
dotnet test SciFiLarpNetwork.slnx --configuration Release
```

Run the frontend's TypeScript tests and checks separately:

```powershell
Set-Location Frontend
npm ci
npm run check
```

The GitHub Actions workflow runs both command paths on pull requests and pushes to `main`. The applications currently have no implementation code, so the generated no-op template tests have been removed rather than counted as meaningful coverage. The xUnit projects compile and provide the test infrastructure; add behavioral tests alongside the domain, event persistence/projections, MVC endpoints, UI workflows, and shared API contracts as those features are implemented. An empty test suite at this scaffold stage does not constitute behavioral test coverage.

## Test design

- Keep domain tests deterministic and fast; test observable behavior and game-rule boundaries, not implementation details.
- Use xUnit for .NET unit, integration, and backend contract tests. Keep frontend tests in the frontend's TypeScript-compatible Vitest suite.
- Integration tests own their data and service lifetime. Use a unique temporary directory/database or isolated test container per test run, clean it up reliably, and never depend on a developer's local event store, credentials, DNS, or machine-specific paths.
- Do not share mutable test state across concurrently executing tests. Avoid timing assumptions, public network calls, real external services, and order-dependent fixtures.
- Exercise the persisted event source, projection rebuilds, optimistic concurrency, MVC behavior, and contracts at appropriate integration boundaries when those implementations exist.
- Keep test data generic and franchise-neutral. Distinguish Account, Player, and Character as defined in the domain glossary.
- Tests and fixture setup must run without public internet access after their declared packages/runtime prerequisites have been prepared locally.

The 80% coverage threshold and coverage reporting policy are specified separately in SPEC-10 and are introduced at its roadmap step.
