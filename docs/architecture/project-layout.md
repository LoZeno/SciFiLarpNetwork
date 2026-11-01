# Project layout

The replacement solution is organized at the repository root. The legacy `SciFiNetwork` implementation tree was removed as part of SPEC-12; historical files remain available through Git history.

```text
SciFiLarpNetwork.slnx
Backend/
  src/            ASP.NET Core MVC backend
  tests/          .NET backend unit and integration tests
Frontend/
  src/            React and TypeScript single-page application
  tests/          Frontend type, unit, and integration tests
  package.json    Frontend scripts and dependencies
Management/
  src/            Avalonia administrative application
  tests/          .NET management-client tests
Localization/     Shared English and Italian resource files
deployment/
  Database/      Database installation and configuration assets
docs/
  architecture/
  specifications/
```

Each application owns its source and tests, so its project, tests, and tooling can be opened and worked on together in the appropriate IDE without mixing in other applications' source trees. Shared repository policy files and the root solution remain at the repository root.

The backend and management application directories are structural placeholders at this stage, not implemented projects. Project files and framework targets are introduced by their corresponding roadmap tasks. The frontend directory contains its tooling baseline; the React application itself is introduced by its roadmap task. Runtime and build dependencies must remain resolvable from the repository and documented SDK/tool installations; no source or build configuration may depend on an external checkout path.

The repository's .NET SDK selection and default target framework are defined by root `global.json` and `Directory.Build.props`; see [the .NET baseline](dotnet-baseline.md) for the documented build command.

Repository-wide nullable, analyzer, and C# style defaults are also defined in `Directory.Build.props` and `.editorconfig`; see [C# conventions](csharp-conventions.md).

The frontend package and its strict TypeScript, lint, formatting, and test-runner configuration live under `Frontend`; see [TypeScript conventions](typescript-conventions.md).

Shared English and Italian localization resources live under `Localization`; see [localization conventions](localization.md).

The separate `Holonet3` source repository remains unchanged and is used only as historical domain/behavior reference material.
