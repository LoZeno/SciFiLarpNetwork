# SPEC-16: Cross-platform CI builds and packaging

## Requirement

Create GitHub Actions workflows that build, test, and package all product components for Windows, Linux, and macOS on x64 and ARM64, including the React frontend, .NET MVC backend, Avalonia management application, and database setup/installer. The approved runtime identifiers are `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`; 32-bit x86 is explicitly excluded by SPEC-07.

## Acceptance criteria

- CI builds and tests the frontend, backend, and management application using reproducible, documented commands.
- Release workflows produce versioned, downloadable artifacts for each supported OS/architecture combination. Artifacts include the relevant application binaries, frontend assets, database installer/configuration, and required runtime prerequisites or clear prerequisite instructions.
- Database distribution is compatible with the selected event-store deployment model and each claimed platform/architecture; database setup, configuration, initialization, upgrade, and backup/restore requirements are documented.
- Workflows use supported GitHub-hosted runners where available. Where a requested architecture or OS cannot be built or exercised by a hosted runner, document the runner or cross-compilation strategy and provide appropriate validation evidence.
- The pipeline reports unsupported combinations explicitly rather than silently omitting them or publishing artifacts as though they were verified.
- Artifacts are smoke-tested after packaging, and release artifact names identify version, OS, architecture, and package format.
- Release bundles and container images contain all mandatory runtime assets and dependencies needed for offline installation; installation must not fetch packages, frontend assets, runtimes, or database services from the public internet (SPEC-21).
- Add an offline validation job or release check that blocks outbound network access during a packaged application smoke test, including local UI asset loading and core API access.
- Secrets are supplied through GitHub Actions secrets or equivalent secure configuration, not committed to workflow files.
