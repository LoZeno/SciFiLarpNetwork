# C# conventions

SDK-style .NET projects inherit repository-wide defaults from `Directory.Build.props`: .NET 10, implicit usings, nullable reference types, the .NET analyzers at the latest recommended analysis level, build-time code-style enforcement, and warnings as errors. Do not disable these globally or suppress warnings broadly; fix the underlying issue or use a narrowly scoped, justified suppression.

The root `.editorconfig` defines consistent whitespace and a small set of C# style rules. From the repository root, run formatting and analyzer checks with:

```powershell
dotnet format SciFiLarpNetwork.slnx --verify-no-changes
dotnet build SciFiLarpNetwork.slnx --configuration Release
```

Follow these design conventions as code is introduced:

- Prefer asynchronous I/O APIs and propagate `CancellationToken` through asynchronous operations.
- Use dependency injection and typed options at application boundaries; keep domain rules explicit and independently testable.
- Keep HTTP, UI, and persistence concerns out of domain behavior. Use explicit domain types and validated inputs where they improve clarity and invariants.
- Use current C# language features when they make code clearer, safer, or more maintainable; do not set a repository-wide language version that can drift from the selected .NET SDK.
- Put each top-level class, record, interface, enum, or struct in its own correspondingly named `.cs` file. When related types form a cohesive group, place their separate files in a focused subfolder rather than combining declarations in one file. Nested types may remain in their containing type when that improves encapsulation.
- Keep namespaces aligned with the file's directory path relative to the project root.
- Use the canonical, franchise-neutral product vocabulary in `domain-language-and-scope.md`. In particular, keep Player, Character, and Account distinct; model configurable Network, Group, and Skill concepts without assuming a specific fictional setting.

Apply this convention to production and test code across both the ASP.NET Core backend and the Avalonia management application.

The root solution currently has no projects, so these checks become effective for C# source as the scheduled backend, management, and test projects are added. Until then, a root solution build succeeds with the documented empty-solution restore warning in `dotnet-baseline.md`.
