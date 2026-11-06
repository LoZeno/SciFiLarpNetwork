# Testing strategy

## Test projects and commands

Backend and Avalonia management tests use xUnit with the .NET 10 test SDK. Backend tests are separated by layer, with database-backed tests clearly identified as integration tests:

- `Backend/tests/SciFiLarpNetwork.Backend.Domain.Tests`
- `Backend/tests/SciFiLarpNetwork.Backend.Application.Tests`
- `Backend/tests/SciFiLarpNetwork.Backend.Infrastructure.Tests`
- `Backend/tests/SciFiLarpNetwork.Backend.Host.Tests`
- `Backend/tests/SciFiLarpNetwork.Backend.Integration.Tests`
- `Management/tests/SciFiLarpNetwork.Management.Tests`

The integration tests require PostgreSQL. From the repository root, start the local database service before running all .NET tests:

```powershell
docker compose up -d
dotnet test SciFiLarpNetwork.slnx --configuration Release
```

Compose initializes separate `scifilarnetwork_dev` and `scifilarnetwork_tests` databases. The integration test fixture defaults to the test database; its connection can be overridden with `ConnectionStrings__EventStore`. Never point that setting at development or production data. Authentication tests create and remove a uniquely named schema and require permission to do so; database-backed test classes run serially to avoid competing projection daemons. The same Docker Compose commands work on Windows, Linux, and macOS, with Docker selecting the native x64 or ARM64 PostgreSQL image.

The Backend Integration test project collects line coverage and writes Cobertura XML and JSON reports under its `TestResults/coverage/` directory; it enforces an 80% line threshold. Only `SciFiLarpNetwork.*` assemblies are measured; test assemblies and compiler-generated code (by generated-code attributes or `.g.cs`, `.generated.cs`, and `.designer.cs` suffixes) are excluded.

Run the frontend's TypeScript tests and checks separately:

```powershell
Set-Location Frontend
npm ci
npm run check
```

The frontend check includes coverage collection. Vitest measures all executable TypeScript under `src/`, writes text, LCOV, and HTML reports under `Frontend/coverage/`, and enforces 80% for both statements and lines.

The GitHub Actions workflow runs both command paths on pull requests and pushes to `master`, and uploads each platform's reports as workflow artifacts. The backend, React SPA, and management application have active tests and coverage thresholds. Add tests alongside domain, event persistence/projections, MVC endpoints, UI workflows, and shared API contracts as those features are implemented.

## Test design

- Keep domain tests deterministic and fast; test observable behavior and game-rule boundaries, not implementation details.
- Use xUnit for .NET unit, integration, and backend contract tests. Keep frontend tests in the frontend's TypeScript-compatible Vitest suite.
- Integration tests own their data and service lifetime. Use the dedicated `scifilarnetwork_tests` database from the local Compose service (or an isolated test container), keep test data isolated from development and production, and never depend on credentials, DNS, or machine-specific paths beyond the documented prerequisites.
- Do not share mutable test state across concurrently executing tests. Avoid timing assumptions, public network calls, real external services, and order-dependent fixtures.
- Exercise the persisted event source, projection rebuilds, optimistic concurrency, MVC behavior, and contracts at appropriate integration boundaries when those implementations exist.
- Event-sourcing test obligations are defined in [event-sourced domain](event-sourced-domain.md): PostgreSQL/Marten store and projection integration cases accompany backend persistence implementation, and aggregate transition cases accompany the relevant feature work. The provider decision is recorded in [datastore decision](datastore-assessment.md).
- Keep test data generic and franchise-neutral. Distinguish Account, Player, and Character as defined in the domain glossary.
- Tests and fixture setup must run without public internet access after their declared packages/runtime prerequisites have been prepared locally.

The 80% coverage thresholds and report formats are specified in SPEC-10.
