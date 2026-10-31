# SPEC-07: Cross-platform and architecture support

## Requirement

The backend, web client, and Avalonia application must support Windows, Linux, and macOS on x86, x64, and ARM64, subject to the actual support matrices of .NET 10, Avalonia, the selected datastore, and their native dependencies.

## Acceptance criteria

- Publish/build and smoke-test results are recorded for each required OS/architecture pair; unsupported pairs are not represented as supported.
- CI validates the full requested matrix where hosted runners and upstream toolchains permit it. Any missing runner coverage has an explicit documented alternative verification.
- Runtime packaging, native dependencies, database setup, file paths, and line endings do not assume Windows or a single CPU architecture.
- If an upstream framework/runtime does not support a requested pair, record the exact limitation and obtain user approval for a compatible alternative before claiming the requirement is met.
