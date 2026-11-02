# Project layout

The replacement solution is organized at the repository root. The legacy `SciFiNetwork` implementation tree was removed as part of SPEC-12; historical files remain available through Git history.

```text
SciFiLarpNetwork.slnx
Backend/
  src/            ASP.NET Core MVC backend
  tests/          .NET backend unit and integration test project
Frontend/
  src/            React and TypeScript single-page application
  tests/          Frontend type, unit, and integration tests
  package.json    Frontend scripts and dependencies
Management/
  src/            Avalonia administrative application
  tests/          .NET management-client test project
Localization/     Shared English and Italian resource files
deployment/
  Database/      Database installation and configuration assets
docs/
  architecture/
  specifications/
```

Each application owns its source and tests, so its project, tests, and tooling can be opened and worked on together in the appropriate IDE without mixing in other applications' source trees. Shared repository policy files and the root solution remain at the repository root.

The backend contains its .NET 10 ASP.NET Core MVC API baseline and xUnit contract tests; domain feature endpoints and authentication are introduced by later roadmap tasks. The frontend contains its React/TypeScript SPA shell, Siren-aware client, localization, and UI tests; domain workflows are introduced by later roadmap tasks. Management remains a structural placeholder until its roadmap task. Runtime and build dependencies must remain resolvable from the repository and documented SDK/tool installations; no source or build configuration may depend on an external checkout path.

The repository's .NET SDK selection and default target framework are defined by root `global.json` and `Directory.Build.props`; see [the .NET baseline](dotnet-baseline.md) for the documented build command.

Repository-wide nullable, analyzer, and C# style defaults are also defined in `Directory.Build.props` and `.editorconfig`; see [C# conventions](csharp-conventions.md).

The frontend package and its strict TypeScript, lint, formatting, and test-runner configuration live under `Frontend`; see [TypeScript conventions](typescript-conventions.md). The SPA shell and Siren client are documented in [React web client](react-web-client.md).

Shared English and Italian localization resources live under `Localization`; see [localization conventions](localization.md).

The .NET xUnit projects, test commands, and test-isolation conventions are described in [testing strategy](testing-strategy.md).

The event/command/aggregate boundaries, stream concurrency contract, schema evolution, and projection recovery rules are described in [event-sourced domain](event-sourced-domain.md).

The selected PostgreSQL + Marten event-store architecture, licensing, ARM64 smoke-test evidence, offline image requirements, and remaining release gates are recorded in [datastore decision](datastore-assessment.md).

The Siren hypermedia API conventions, relation vocabulary, action mapping, localization, and error format are defined in the [Siren API profile](siren-api-profile.md).

The ASP.NET Core MVC backend and xUnit contract tests are implemented under `Backend/src` and `Backend/tests`; see [SPEC-05](../specifications/SPEC-05-aspnetcore-mvc-backend.md) for the initial API contract and remaining feature implementation.

Backend run/configuration guidance and the current API endpoints are recorded in [backend API baseline](backend-api.md).

Offline runtime constraints and the current frontend asset guard are defined in [closed-intranet operation](offline-operation.md).

The verified OS/architecture constraints and platform approval blocker are recorded in the [platform support matrix](platform-support-matrix.md).

The separate `Holonet3` source repository remains unchanged and is used only as historical domain/behavior reference material.
