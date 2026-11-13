# SPEC-24: Durable event-store runtime

## Purpose and prerequisites

Configure the selected PostgreSQL 18 + Marten 9 event store for the .NET 10 backend and verify the Marten capabilities that later features rely on. SPEC-09 selected the provider; this specification delivers the runtime needed before the first event-sourced product feature. Follow [SPEC-08](SPEC-08-event-sourced-domain.md) and [the event-sourced domain architecture](../architecture/event-sourced-domain.md).

Marten is the event store, not an adapter behind a project-defined port. Do not wrap it in a provider-neutral stream-store abstraction or re-implement capabilities Marten already provides (event identity, stream versioning, optimistic concurrency, metadata, upcasting, projection checkpoints and rebuilds). Use the documented Marten APIs; [the Marten LLM documentation](../references/marten-llms.txt) is the reference.

This is a backend foundation, not a feature-specific aggregate or a substitute for later feature acceptance criteria. No production command may claim success using an in-memory store or mutable current-state-only persistence.

## Acceptance criteria

- Register and configure Marten 9 with PostgreSQL 18 through `AddMarten` in the backend's normal dependency injection and configuration. Database connection details are supplied securely per deployment; no connection secret is committed or logged.
- Apply the configured schema explicitly and repeatably (`IDocumentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync()`). Fail startup or readiness clearly when schema setup or database connectivity is unavailable; do not silently fall back to another store.
- Use Guid stream identity and register every event type with `Events.AddEventType<T>()` so stored event type names do not depend on CLR namespaces or assemblies.
- Enable the event metadata the domain needs (user name, correlation and causation ids). Commands attribute their events by setting `IDocumentSession.LastModifiedBy`: HTTP-originated sessions use the authenticated actor and never a client-supplied identity; system-originated work leaves it empty.
- Use `EventAppendMode.Rich` so `Inline` projections see final event versions and sequences.
- Command handlers follow Marten's [command handler workflow](https://martendb.io/scenarios/command_handler_workflow.html): load each aggregate with `FetchForWriting<TAggregate>()`, append to the returned streams, and commit with one `SaveChangesAsync()`. A `ConcurrencyException` (including `EventStreamUnexpectedMaxEventIdException`) means nothing was written; the command is rejected and may be re-evaluated from fresh state.
- Command retries are made safe as Marten documents, not with a custom receipt store: creation commands use a deterministic stream id (a repeat raises `ExistingStreamIdCollisionException`) and updates carry the expected stream version (a repeat raises `ConcurrencyException`).
- Evolve event schemas with Marten's event type mapping and upcasting (`Events.MapEventType`, `Events.Upcast<TOld, TNew>`, `EventUpcaster<TOld, TNew>`), never by rewriting stored events. Never persist plaintext credentials, session tokens, QR authentication secrets, or other reusable secrets in events.
- Run asynchronous projections with Marten's async daemon. Each projection's lifecycle (`Inline`, `Async`, or `Live`) is chosen by its feature specification: write models that commands decide on must be `Inline` or fetched through `FetchForWriting`. Projections can be rebuilt from the event history, and a projection failure must not discard committed events.
- Make database availability and projection failure observable through the backend's health/readiness and operational diagnostics without exposing secrets or event payloads.
- Preserve offline/intranet operation: runtime startup, event writes, reads, and projection work do not require a public service or package download.

## Verification

- Integration tests use the real PostgreSQL/Marten configuration. Reset state between tests with Marten's documented helpers (`IDocumentStore.Advanced.ResetAllData()`, or `IHost.ResetAllMartenDataAsync()` when async projections are running) and wait for async projections with `WaitForNonStaleProjectionDataAsync`.
- Tests prove an asynchronous projection rebuilt from history matches the incrementally built state, and that a failing projection does not discard committed events and recovers through a rebuild.
- Tests prove HTTP-originated events are attributed to the authenticated actor.
- Each feature specification adds its own aggregate, concurrency-conflict, retry, and upcaster tests. The tests verify this application's configuration and behavior; they do not re-test Marten's storage guarantees.
- CI provisions the supported PostgreSQL service for integration tests or explicitly reports a clear infrastructure failure; no test-only in-memory provider may stand in for the integration suite.
- Keep the repository's coverage thresholds, xUnit conventions, and supported-platform/offline requirements.
