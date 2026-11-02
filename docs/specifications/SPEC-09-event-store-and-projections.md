# SPEC-09: Event store and projections

## Decision

Use PostgreSQL 18 with Marten 9 for the durable event store and projections. PostgreSQL event streams are the source of truth; read models are rebuildable. Run PostgreSQL in a Linux container with Docker Compose/operator-provided container runtime. The decision, license review, ARM64 host smoke test, projection strategy, and remaining release gates are recorded in [datastore decision](../architecture/datastore-assessment.md). Do not add RavenDB or a second datastore.

## Step 14 acceptance criteria — provider selection

- Select a provider compatible with .NET 10 and record datastore and library redistribution licenses.
- Establish that the database image supports the approved host CPU architectures and verify a native startup on the user's Windows ARM64 Docker host.
- Define the event append/concurrency contract, projection lifecycles/freshness, offline image provisioning, and deployment risks before backend implementation.
- Keep all six application RIDs and 32-bit x86 exclusion aligned with SPEC-07.

All four criteria are satisfied by the decision and evidence in [datastore decision](../architecture/datastore-assessment.md). The successful local PostgreSQL 18.6 smoke test ran on Docker's `linux/arm64` engine. This closes provider selection; it does not claim that later product implementation or release gates are complete.

## Implementation requirements for subsequent backend/deployment work

These are product implementation and release gates, not evidence that the durable adapter already exists:

- Store immutable, replayable events with stream identity/version, explicit event and schema types, timestamps, actor/correlation metadata, and payload; atomically append with expected-version concurrency.
- Build rebuildable, idempotent projections. Specify inline/async/live consistency per use case, surface async checkpoints and errors, and support clean rebuild and failure recovery.
- Test ordering, replay, concurrency conflicts, schema evolution, duplicate delivery, projection rebuild, and recovery with xUnit and the selected PostgreSQL/Marten integration.
- Document pinned image setup, offline import/start, secure configuration, persistence, backup/restore, and upgrade procedures. Later release work must test all six approved RIDs and outbound-blocked startup with locally available images.
