# SPEC-17: Configuration documentation in README

## Requirement

The repository root `README.md` documents how to configure, build, run, and troubleshoot each product component and supported hosting mode.

## Acceptance criteria

- Document prerequisites and configuration for the React frontend, .NET MVC backend, Avalonia management client, and selected database/event store.
- Explain required settings, their purpose, safe local-development defaults, and how to provide secrets without committing them.
- Include setup and run instructions for local development and the self-hosted deployment described by SPEC-18, plus container deployment described by SPEC-19 if implemented.
- Document platform/architecture support, package/artifact installation, database initialization and upgrades, and backup/restore.
- Document that English is the default language, list supported languages (at least English and Italian), and explain how users or operators select/configure the language.
- Document closed-intranet/offline installation and configuration, local network addressing, locally served frontend assets, offline dependencies, and how to verify operation without outbound internet access (SPEC-21).
- Commands and configuration examples match the implemented project layout and are verified during implementation; README must not claim unsupported platforms or modes.
