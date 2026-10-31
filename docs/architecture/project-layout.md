# Project layout

The replacement solution is organized at the repository root and kept separate from the legacy `SciFiNetwork` tree until the greenfield replacement task (SPEC-12) explicitly addresses that content.

```text
SciFiLarpNetwork.sln
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

The existing `SciFiNetwork` solution, its projects, databases, binaries, and metadata are intentionally untouched by this layout task.
