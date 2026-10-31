# SPEC-03: .NET 10

## Requirement

All C# application and test projects target .NET 10 using SDK-style project files and supported .NET 10 APIs. Do not retain .NET Framework projects or Windows-only framework dependencies.

## Acceptance criteria

- The solution builds with the .NET 10 SDK using documented commands.
- Backend, shared .NET libraries, Avalonia application, and .NET test projects target `net10.0` or an appropriate `net10.0-*` target when required by a supported platform-specific UI dependency.
- Dependencies are actively maintained, compatible with the selected target frameworks, and do not silently introduce a narrower OS or architecture support matrix.
