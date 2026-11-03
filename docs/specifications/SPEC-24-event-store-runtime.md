# SPEC-24: Durable event-store runtime

## Purpose and prerequisites

Implement the selected durable PostgreSQL 18 + Marten 9 event-store provider for the .NET 10 backend. SPEC-09 selected the provider and documented its architecture; this specification delivers the runtime needed before the first event-sourced product feature. Follow the provider-neutral contracts in [SPEC-08](SPEC-08-event-sourced-domain.md) and [the event-sourced domain architecture](../architecture/event-sourced-domain.md).

This is a backend foundation, not a feature-specific aggregate or a substitute for later feature acceptance criteria. No production command may claim success using an in-memory store or mutable current-state-only persistence.

## Acceptance criteria

- Register and configure Marten 9 with PostgreSQL 18 through the backend's normal dependency injection and configuration. Database connection details are supplied securely per deployment; no connection secret is committed or logged.
- Provide an explicit, repeatable database schema setup/versioning process. Fail startup or readiness clearly when required schema setup or database connectivity is unavailable; do not silently fall back to another store.
- Provide a provider-neutral application boundary for loading ordered event streams and atomically appending a command's event batch with an expected stream version. An append conflict leaves the stream unchanged and is surfaced for command re-evaluation.
- Persist globally unique event identity, stable stream identity, contiguous stream sequence, explicit stable event type and positive schema version, UTC occurrence/recorded timestamps, actor/correlation/causation metadata, and immutable payloads.
- Support deterministic replay, event-schema evolution/upcasting without rewriting stored events, and idempotent command/request deduplication where a command requires it. Never persist plaintext credentials, session tokens, QR authentication secrets, or other reusable secrets in events.
- Establish rebuildable projection infrastructure with explicit projection versions, checkpoints, duplicate-delivery handling, observable errors/lag, clean rebuild, and recovery. Feature-specific projections and freshness contracts are implemented with their feature specifications.
- Make database availability and projection failure observable through the backend's health/readiness and operational diagnostics without exposing secrets or event payloads.
- Preserve offline/intranet operation: runtime startup, event writes, reads, and projection work do not require a public service or package download.

## Verification

- xUnit tests cover empty and existing streams, ordered replay, contiguous sequence checks, duplicate/gap rejection, event metadata, and deterministic upcasting.
- PostgreSQL/Marten integration tests prove atomic append, expected-version concurrency conflicts, duplicate command handling, projection idempotency, clean rebuild equivalence, and failure/recovery behavior against the selected provider.
- Tests prove a failed append or projection never appears as a successful command and that a projection error does not discard an already committed event.
- CI provisions the supported PostgreSQL service for integration tests or explicitly reports a clear infrastructure failure; no test-only in-memory provider may stand in for the provider integration suite.
- Keep the repository's coverage thresholds, xUnit conventions, and supported-platform/offline requirements.
