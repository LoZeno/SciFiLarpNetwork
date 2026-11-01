# Datastore decision

## Decision

Use **PostgreSQL 18 in the official multi-architecture Linux container image, with Marten 9 as the .NET event store and projection framework**. PostgreSQL event streams are the durable source of truth; Marten projections provide rebuildable read models. Do not add RavenDB or a second datastore.

The database runs in a container on the host. On Linux, use a compatible container engine; on Windows and macOS, use Docker Desktop or another compatible runtime. The product packages Compose configuration and setup guidance, not Docker Desktop itself. A venue can prepare/pull the image before going offline, or install it from a supplied offline image archive.

## Evidence reviewed 2026-10-31

- The PostgreSQL 18.6 official image publishes both `linux/amd64` and `linux/arm64` variants. Its image index was inspected directly; the selected ARM64 image digest was `sha256:9a74990a5c776ff210361b67616b347750c66c36ea0dfbb4f575d19fcc3d8437`.
- On the developer's Snapdragon X2 Elite/Windows ARM64 host, Docker Desktop reports a `linux/aarch64` engine. PostgreSQL 18.6 was started in that engine, reported `linux/arm64`, passed `pg_isready`, and answered a SQL query reporting version `18.6`.
- This verifies the immediate ARM64 development-host database path. It does not substitute for the product's later release-matrix, persistence, backup/restore, and offline smoke tests.
- Docker's official PostgreSQL image metadata lists both x86-64 and ARM64. Those Linux images can serve the six approved host/RID combinations through native Linux containers or the host's Linux-container runtime. A database image's architecture is distinct from the backend's .NET RID.
- Docker currently labels Docker Desktop for Windows Arm as **Early Access**. It works on the development host, but that status remains an operator prerequisite and release risk; do not claim a generally supported Windows ARM deployment solely from the successful local smoke test.
- Marten 9.3.0's NuGet metadata declares `net10.0` compatibility. Marten is MIT-licensed. PostgreSQL is under the PostgreSQL License, which permits use, modification, and redistribution subject to retaining its notices. No datastore-specific online activation or paid server license is required.
- Docker Desktop has its own terms. It is not redistributed with the product; each operator must obtain and use a suitable container runtime under its applicable terms.

## Event and projection design

- Store immutable, versioned domain events in Marten streams. Persist stream identity and sequence, explicit event type/schema version, UTC occurrence/recording times, actor/correlation/causation metadata, and replay-sufficient JSON payloads as defined by [event-sourced domain](event-sourced-domain.md).
- Append event batches atomically using Marten's expected stream version. A stale expected version is a concurrency conflict: reload the stream and re-evaluate the command, never blindly retry stale events.
- Keep projections as rebuildable C#-defined read models. Prefer Marten's projection support over hand-maintained current-state tables. Use inline projections when the command requires the updated view in the same transaction; use asynchronous projections for read views that can tolerate lag; reserve live aggregation for suitable on-demand queries.
- Every asynchronous projection must expose its checkpoint/freshness and failure state. Projection updates must be idempotent and rebuildable from the event history. Read-your-writes callers must be able to wait for or observe a checkpoint covering their committed event.
- Keep event and projection schema evolution explicit. PostgreSQL documents, projections, and snapshots are derived from event history and are not an alternate write authority.

## Licensing and offline operation

PostgreSQL and Marten have redistributable open-source licenses; preserve the required upstream notices in release artifacts. Docker Desktop is installed separately by the operator and is not included in redistributables. No runtime license activation is needed for PostgreSQL or Marten.

Offline deployment must have the exact database image available locally before startup. A prepared bundle must include or reference a locally imported image and must not silently pull from a public registry at venue runtime. Keep application-to-database traffic on a private container network, persist database files in a named volume or explicitly selected durable host path, and provide database credentials through operator-managed configuration rather than committed defaults.

Back up with PostgreSQL-supported logical or physical backup procedures and verify restoration; do not copy a live data directory as a backup. Pin a supported PostgreSQL major and image digest for each release, document upgrades, and test the upgrade/restore path before changing the pinned image.

## Remaining product gates

The datastore decision is complete, but these are not claimed as completed product verification:

- Exercise application persistence, concurrency, projection rebuild/failure recovery, backup/restore, and restart durability in automated tests.
- Build and smoke-test the full application/deployment on all six SPEC-07 RIDs. Validate each host's container runtime and the database image architecture; Windows ARM must account for Docker Desktop's Early Access status.
- Perform SPEC-21's outbound-blocked deployment test with the database image already available locally.
- Before release, pin the actual PostgreSQL image by digest, include its notices, provide backup/restore and upgrade instructions, and review the applicable Docker Desktop terms for the intended operator.

## Sources

- [PostgreSQL official image and architecture metadata](https://github.com/docker-library/official-images/blob/master/library/postgres)
- [PostgreSQL license](https://www.postgresql.org/about/licence/)
- [Marten 9.3.0 NuGet package and supported frameworks](https://www.nuget.org/packages/Marten/9.3.0)
- [Marten license](https://github.com/JasperFx/marten/blob/master/LICENSE)
- [Marten event store](https://martendb.io/events/)
- [Marten projections](https://martendb.io/events/projections/)
- [Docker Desktop for Windows installation, terms, and Windows Arm status](https://docs.docker.com/desktop/setup/install/windows-install/)
