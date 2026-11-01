# Project layout

The replacement solution is organized at the repository root. The legacy `SciFiNetwork` implementation tree was removed as part of SPEC-12; historical files remain available through Git history.

```text
SciFiLarpNetwork.slnx
src/
  Backend/       ASP.NET Core MVC backend
  Frontend/      React and TypeScript single-page application
  Management/    Avalonia administrative application
tests/
  Backend/       .NET backend unit and integration tests
  Frontend/      Frontend type, unit, and integration tests
  Management/    .NET management-client tests
deployment/
  Database/      Database installation and configuration assets
docs/
  architecture/
  specifications/
```

The application and test directories are structural placeholders at this stage, not implemented projects. Project files and framework targets are introduced by their corresponding later roadmap tasks. Runtime and build dependencies must remain resolvable from the repository and documented SDK/tool installations; no source or build configuration may depend on an external checkout path.

The repository's .NET SDK selection and default target framework are defined by root `global.json` and `Directory.Build.props`; see [the .NET baseline](dotnet-baseline.md) for the documented build command.

Repository-wide nullable, analyzer, and C# style defaults are also defined in `Directory.Build.props` and `.editorconfig`; see [C# conventions](csharp-conventions.md).

The separate `Holonet3` source repository remains unchanged and is used only as historical domain/behavior reference material.
