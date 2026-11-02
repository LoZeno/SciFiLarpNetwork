# SPEC-13: Idiomatic modern C#

## Requirement

Write C# in current, idiomatic .NET 10 style and use modern language/runtime features where they make the code clearer, safer, or more maintainable.

## Acceptance criteria

- Nullable reference types and analyzers are enabled for first-party projects; warnings are addressed rather than broadly suppressed.
- Use async I/O, cancellation, dependency injection, typed options, and explicit domain types where appropriate.
- Keep domain rules explicit and testable; avoid leaking persistence or HTTP concerns into domain behavior.
- Formatting, analyzers, and build checks are automated and consistently applied.

## Baseline implementation

- Root `Directory.Build.props` enables nullable reference types, implicit usings, .NET analyzers at the latest recommended analysis level, build-time code-style enforcement, and warnings as errors for SDK-style .NET projects.
- Root `.editorconfig` establishes consistent whitespace and baseline C# style rules.
- [C# conventions](../architecture/csharp-conventions.md) documents the design guidance and root formatting/build commands.
- Project-specific configuration must preserve these defaults unless a documented, narrowly scoped technical constraint requires an exception.
