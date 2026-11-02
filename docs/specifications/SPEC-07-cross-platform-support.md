# SPEC-07: Cross-platform and architecture support

## Requirement

The backend, web client, and Avalonia application must support Windows, Linux, and macOS on x64 and ARM64, subject to the actual support matrices of .NET 10, Avalonia, the selected datastore, and their native dependencies. The user approved excluding 32-bit x86 on 2026-10-31 after upstream review showed it is unavailable for .NET 10 Linux/macOS and Avalonia classifies Windows x86 desktop support as commercial-only Tier 3.

## Acceptance criteria

- The approved target matrix is exactly `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`; 32-bit x86 is not represented as supported.
- Upstream framework/runtime constraints are reviewed and recorded before selecting dependent packages or making support claims.
- Publish/build and smoke-test results are recorded for every approved OS/architecture pair before product release; unsupported or unverified pairs are not represented as shipped support.
- CI validates the full requested matrix where hosted runners and upstream toolchains permit it. Any missing runner coverage has an explicit documented alternative verification.
- Runtime packaging, native dependencies, database setup, file paths, and line endings do not assume Windows or a single CPU architecture.
- If an upstream framework/runtime does not support a requested pair, record the exact limitation and obtain user approval for a compatible alternative before claiming the requirement is met.

## Platform review

- The upstream .NET 10 and Avalonia matrices have been reviewed and recorded in the [platform support matrix](../architecture/platform-support-matrix.md).
- The review identifies 32-bit x86 as unsupported by the official .NET 10 Linux/macOS runtime distributions, and Windows x86 Avalonia desktop support as Tier 3 (commercial support only). The user approved a 64-bit-only matrix.
- The framework-level matrix and scope decision are complete. Product-level build/publish and smoke-test evidence remains a release gate for the later Avalonia, deployment, and CI tasks; until that evidence exists, do not claim a target as shipped support.
