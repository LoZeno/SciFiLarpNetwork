# .NET SDK and target-framework baseline

The repository uses the .NET 10 SDK. `global.json` requests the .NET 10.0.1xx feature band and rolls forward to a later installed .NET 10 feature band, while avoiding prerelease SDKs.

`Directory.Build.props` gives first-party SDK-style .NET projects a default target framework of `net10.0`. A project may use a more specific .NET 10 target framework when a supported dependency requires it. Backend, management, and test project files are introduced by their respective roadmap specifications.

From the repository root, verify the selected SDK and build the solution:

```powershell
dotnet --version
dotnet build SciFiLarpNetwork.slnx
```

The solution currently contains no projects by design; later implementation steps add projects and build coverage. Until then, `dotnet build` succeeds but emits the SDK warning `Unable to find a project to restore!`. The command remains the root solution build entry point.
