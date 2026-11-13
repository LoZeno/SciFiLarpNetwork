# Testing strategy

## Test projects and commands

Backend and Avalonia management tests use xUnit.net v3 with Microsoft Testing Platform (MTP) and the .NET 10 SDK. `global.json` selects MTP for `dotnet test`; test projects use the standard `xunit.v3` package without the VSTest SDK or Visual Studio adapter. Backend tests are separated by layer, with database-backed tests clearly identified as integration tests:

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

Run the frontend's TypeScript tests and checks separately:

```powershell
Set-Location Frontend
npm ci
npm run check
```

The GitHub Actions workflow runs both command paths on pull requests and pushes to `master`. Add tests alongside domain, event persistence/projections, MVC endpoints, UI workflows, and shared API contracts as those features are implemented.

## Coverage gates

CI enforces two separate gates, specified in [SPEC-10](../specifications/SPEC-10-code-coverage.md):

| Gate | What is measured | Minimum | Enforced by |
|---|---|---|---|
| C# | All production C#, by all .NET test projects together | 80% lines | ReportGenerator, after the .NET tests |
| TypeScript | All production TypeScript in `Frontend/src` | 80% statements and lines | Vitest, inside `npm run check` |

### C# gate

Each .NET test project writes its own coverage report to its `TestResults/coverage/` directory. No single project has a threshold: unit tests and integration tests cover different parts of the code, so only their combined coverage is checked. Coverage includes the `SciFiLarpNetwork.*` production assemblies and excludes test assemblies and generated code (generated-code attributes, and `.g.cs`, `.generated.cs`, and `.designer.cs` files).

To check the C# gate locally, run the .NET tests (see above), then merge the reports:

```powershell
dotnet tool restore
dotnet reportgenerator "-reports:Backend/tests/**/TestResults/coverage/*.cobertura.*.xml;Management/tests/**/TestResults/coverage/*.cobertura.*.xml" "-targetdir:coverage/dotnet" "-reporttypes:Cobertura;HtmlSummary;MarkdownSummaryGithub;TextSummary" "minimumCoverageThresholds:lineCoverage=80"
```

The command fails when combined line coverage is below 80%. It writes the combined report to `coverage/dotnet/` either way; open `summary.html` or `Summary.txt` to see coverage per assembly and class. CI runs the same command, adds the summary to the job page, and uploads all reports as the `dotnet-coverage` artifact.

### TypeScript gate

`npm run check` in `Frontend` runs the tests with coverage and fails when statement or line coverage is below 80%. Reports are written to `Frontend/coverage/`; CI uploads them as the `frontend-coverage` artifact.

## Test design

- Keep domain tests deterministic and fast; test observable behavior and game-rule boundaries, not implementation details.
- Use xUnit.net v3 on Microsoft Testing Platform for .NET unit, integration, and backend contract tests. Keep frontend tests in the frontend's TypeScript-compatible Vitest suite.
- Integration tests own their data and service lifetime. Use the dedicated `scifilarnetwork_tests` database from the local Compose service (or an isolated test container), keep test data isolated from development and production, and never depend on credentials, DNS, or machine-specific paths beyond the documented prerequisites.
- Do not share mutable test state across concurrently executing tests. Avoid timing assumptions, public network calls, real external services, and order-dependent fixtures.
- Exercise the persisted event source, projection rebuilds, optimistic concurrency, MVC behavior, and contracts at appropriate integration boundaries when those implementations exist.
- Event-sourcing test obligations are defined in [event-sourced domain](event-sourced-domain.md): PostgreSQL/Marten store and projection integration cases accompany backend persistence implementation, and aggregate transition cases accompany the relevant feature work. The provider decision is recorded in [datastore decision](datastore-assessment.md).
- Keep test data generic and franchise-neutral. Distinguish Account, Player, and Character as defined in the domain glossary.
- Tests and fixture setup must run without public internet access after their declared packages/runtime prerequisites have been prepared locally.

The coverage gates are specified in SPEC-10 and described under [Coverage gates](#coverage-gates).
