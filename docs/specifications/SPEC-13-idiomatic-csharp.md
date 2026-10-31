# SPEC-13: Idiomatic modern C#

## Requirement

Write C# in current, idiomatic .NET 10 style and use modern language/runtime features where they make the code clearer, safer, or more maintainable.

## Acceptance criteria

- Nullable reference types and analyzers are enabled for first-party projects; warnings are addressed rather than broadly suppressed.
- Use async I/O, cancellation, dependency injection, typed options, and explicit domain types where appropriate.
- Keep domain rules explicit and testable; avoid leaking persistence or HTTP concerns into domain behavior.
- Formatting, analyzers, and build checks are automated and consistently applied.
