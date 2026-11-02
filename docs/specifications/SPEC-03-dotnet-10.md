# SPEC-03: .NET 10

## Requirement

All C# application and test projects target .NET 10 using SDK-style project files and supported .NET 10 APIs. Do not retain .NET Framework projects or Windows-only framework dependencies.

## Acceptance criteria

- The solution builds with the .NET 10 SDK using documented commands.
- Backend, shared .NET libraries, Avalonia application, and .NET test projects target `net10.0` or an appropriate `net10.0-*` target when required by a supported platform-specific UI dependency.
- Dependencies are actively maintained, compatible with the selected target frameworks, and do not silently introduce a narrower OS or architecture support matrix.

## Baseline implementation

- The root `global.json` selects the .NET 10 SDK feature band and permits forward selection of later .NET 10 feature bands.
- Root `Directory.Build.props` sets `net10.0` as the default target framework for SDK-style .NET projects; project files may select an appropriate more-specific .NET 10 target framework when needed.
- Backend, Avalonia, and test project files are intentionally introduced by their respective roadmap specifications.
- See [the documented .NET build baseline](../architecture/dotnet-baseline.md).
