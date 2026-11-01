# Testing strategy

## Test projects and commands

Backend and Avalonia management tests use xUnit with the .NET 10 test SDK. Test projects live with their application:

- `Backend/tests/SciFiLarpNetwork.Backend.Tests`
- `Management/tests/SciFiLarpNetwork.Management.Tests`

Run all .NET tests from the repository root:

```powershell
dotnet test SciFiLarpNetwork.slnx --configuration Release
```

The .NET test projects collect line coverage and write Cobertura XML and JSON reports under each project's `TestResults/coverage/` directory. The console summary is the human-readable report. The 80% line threshold is enforced for each application test project when its corresponding production project (`Backend/src/SciFiLarpNetwork.Backend.csproj` or `Management/src/SciFiLarpNetwork.Management.csproj`) is introduced. Only `SciFiLarpNetwork.*` assemblies are measured; test assemblies and compiler-generated code (by generated-code attributes or `.g.cs`, `.generated.cs`, and `.designer.cs` suffixes) are excluded.

Run the frontend's TypeScript tests and checks separately:

```powershell
Set-Location Frontend
npm ci
npm run check
```

The frontend check includes coverage collection. Vitest measures all executable TypeScript under `src/`, writes text, LCOV, and HTML reports under `Frontend/coverage/`, and enforces 80% for both statements and lines.

The GitHub Actions workflow runs both command paths on pull requests and pushes to `master`, and uploads each platform's reports as workflow artifacts. At this scaffold stage there is no production C# or TypeScript source to measure; the .NET projects emit empty reports without applying a meaningless zero-code threshold, and the frontend coverage include set is empty. This is not behavioral coverage. Add tests alongside domain, event persistence/projections, MVC endpoints, UI workflows, and shared API contracts as those features are implemented; introducing either production project activates its threshold.

## Test design

- Keep domain tests deterministic and fast; test observable behavior and game-rule boundaries, not implementation details.
- Use xUnit for .NET unit, integration, and backend contract tests. Keep frontend tests in the frontend's TypeScript-compatible Vitest suite.
- Integration tests own their data and service lifetime. Use a unique temporary directory/database or isolated test container per test run, clean it up reliably, and never depend on a developer's local event store, credentials, DNS, or machine-specific paths.
- Do not share mutable test state across concurrently executing tests. Avoid timing assumptions, public network calls, real external services, and order-dependent fixtures.
- Exercise the persisted event source, projection rebuilds, optimistic concurrency, MVC behavior, and contracts at appropriate integration boundaries when those implementations exist.
- Event-sourcing test obligations are defined in [event-sourced domain](event-sourced-domain.md): PostgreSQL/Marten store and projection integration cases accompany backend persistence implementation, and aggregate transition cases accompany the relevant feature work. The provider decision is recorded in [datastore decision](datastore-assessment.md).
- Keep test data generic and franchise-neutral. Distinguish Account, Player, and Character as defined in the domain glossary.
- Tests and fixture setup must run without public internet access after their declared packages/runtime prerequisites have been prepared locally.

The 80% coverage thresholds and report formats are specified in SPEC-10.
