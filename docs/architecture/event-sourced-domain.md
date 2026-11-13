# Event-sourced domain

The backend project dependency and folder boundaries are defined in [backend DDD structure](backend-ddd-structure.md).

## Authority and responsibilities

The append-only event history is the authoritative record of accepted domain changes. A projection, snapshot, cache, or user-interface view is derived state: it may be discarded and rebuilt, and it must never be treated as the only surviving record of a change.

The write path is:

1. A client submits a command expressing intent, not a pre-built event or a replacement entity document.
2. The application layer authenticates the actor, checks authorization and command-level input constraints, opens one Marten session, and loads each aggregate it decides on with `FetchForWriting<TAggregate>()`, which also records that stream's current version.
3. The aggregate state decides whether the requested transition is valid. The handler appends zero or more immutable domain events to the fetched streams (or starts new streams); invalid transitions append nothing.
4. One `SaveChangesAsync()` commits every appended event, every new stream, and every `Inline` projection update in a single database transaction, and only if each fetched stream is still at the version that was read. A conflict raises Marten's `ConcurrencyException` and nothing is written. The caller reloads and re-evaluates the intent; it must not blindly retry stale events.
5. Projections consume committed events in stream order and expose rebuildable query models.

Transport, authentication, persistence-provider APIs, and projection storage do not belong in domain aggregates. Command handlers use Marten directly, following the documented [command handler workflow](https://martendb.io/scenarios/command_handler_workflow.html); there is no separate provider-neutral event-store port, because PostgreSQL + Marten is the only supported store (SPEC-09).

## Commands, streams, and transaction boundaries

- Commands are explicit requests such as `RegisterCharacterForEvent`; they are not persisted and are not trusted as proof that an operation succeeded.
- An aggregate is the consistency boundary for a set of invariants that must be decided atomically. Keep aggregates small enough to avoid unrelated write conflicts. Do not prescribe an aggregate per database document or combine all LARP data into one aggregate.
- Each aggregate instance has one stable stream identifier and a monotonically increasing stream version. Sequence numbers start at 1 and are contiguous; a stream's expected version is 0 when it has no events.
- A command may append to several streams in one Marten session. The session commits all of them atomically, and the optimistic-concurrency check applies to every stream fetched with `FetchForWriting`. A rejected or conflicting commit leaves every stream unchanged. When a decision depends on a stream the command does not append to, set `AlwaysEnforceConsistency = true` on that fetched stream so a concurrent change to it still rejects the commit.
- Work that cannot be completed inside one session (for example, a step that waits for another actor or an external system) is not an implicit transaction. Make its consistency model and failure recovery explicit; use an idempotent process/workflow and compensating domain events where appropriate.
- Retrying a command after a concurrency conflict means reloading state and rerunning authorization and domain decisions.
- Marten does not deduplicate commands, and a commit is not idempotent: replaying one appends its events again. Make retries safe the way Marten documents: give creation commands a deterministic stream id so a repeat fails with `ExistingStreamIdCollisionException`, and let updates carry the expected stream version so a repeat fails with `ConcurrencyException`.

Aggregate boundaries are chosen with each capability's invariants, not fixed prematurely for all features. Candidate areas include Account, Player, Character, Network, configurable Group and Skill catalogs, Event and its days/participation, messages and per-character mailbox state, news stories, personal files/datapads, and item/substance catalogs. Their final boundaries are established alongside the corresponding feature specifications. Cross-references use stable identifiers; they do not merge the glossary's distinct Account, Player, and Character concepts.

## Event contract and history integrity

Every stored event has (Marten stores these in `mt_events`; see [event storage](https://martendb.io/events/storage.html) and [event metadata](https://martendb.io/events/metadata.html)):

| Metadata | Meaning |
|---|---|
| Event ID | Globally unique identity for audit (`mt_events.id`). |
| Stream ID and version | Aggregate identity and contiguous ordering within that stream (`stream_id`, `version`). The store-wide `seq_id` is a different number and is never used as a stream version. |
| Event type name | Stable, explicit name in the `type` column, independent of the CLR type's namespace or assembly. Register every event type with `Events.AddEventType<T>()`; pin a name with `Events.MapEventType<T>("name")` when a class is renamed. |
| Occurred/recorded time | UTC instants; the domain occurrence time is carried in the payload, and the store acceptance time is Marten's `timestamp`. |
| Actor, correlation, and causation IDs | Auditable initiator (`IDocumentSession.LastModifiedBy`, stored as the event user name) and links between commands/events, nullable only for explicitly system-originated work. |
| Payload | The immutable facts needed to replay the accepted transition, without transport or projection-only fields. |

Events are append-only through normal application operations. Corrections, reversals, administrative changes, and fictional breach effects are new, auditable events; they do not edit or delete earlier facts. Event payloads must not contain plaintext passwords, session/bearer tokens, QR identity secrets, or other reusable authentication secrets. Authentication verifier storage and retention must be designed with the authentication specification rather than copied casually into projections or audit metadata.

## Replay, schema evolution, and snapshots

- Replay is performed by Marten (`FetchForWriting`, `AggregateStreamAsync`, and projections). Application code does not re-implement stream reading or sequence validation.
- Event schemas evolve as described in Marten's [events versioning](https://martendb.io/events/versioning.html) guide. Prefer non-breaking changes (a new nullable property, or a new required property with a default). A breaking change introduces a new event CLR type and registers an upcaster from the old type with `Events.Upcast<TOld, TNew>(...)` or an `EventUpcaster<TOld, TNew>` class. There is no separate numeric schema version: the stored event type name identifies the schema.
- Upcasters are deterministic, pure transformations applied when events are read. They do not rewrite stored history. Keep representative historical payload fixtures so supported event types remain replayable.
- Snapshots are optional performance hints tagged with stream identity, the exact version through which they were built, and a snapshot schema version. Replay verifies the snapshot boundary and applies only later events. A snapshot can always be discarded and reconstructed from the event history.
- Projection definitions have their own version. A rebuild starts from a clean projection generation (or isolated target), replays the committed history in order, and only publishes the new generation when it has reached its declared completion boundary.

## Projections, failures, and freshness

Projections are query-side models, not domain authorities. Each projection declares its input event types, output model, processing/checkpoint strategy, and consistency/freshness contract. The default is eventual consistency; callers that need read-your-writes behavior must be able to request or observe a checkpoint at least as recent as the command's committed stream version.

Projection application is idempotent by event identity or checkpoint position. Advance a checkpoint only after the corresponding projection update succeeds. A projection failure is observable and retains the failed position and error; it must not roll back or discard a committed event. Recovery retries an idempotent update or rebuilds into a clean generation from the durable history. Monitoring exposes the last successful checkpoint, lag/failure state, and rebuild outcome. A failed or stale projection must not be reported as fresh.

## Domain invariants and product constraints

- Account, Player, Character, and in-game network accounts remain distinct identities with explicit relationships.
- Network, Group, Skill, labels, and fictional content are configurable. No event name, invariant, seeded event, or identifier assumes a franchise-specific setting.
- Event registration/participation, message delivery/read state, ownership, authorization-sensitive operations, and any later simulated hacking effects are recorded as domain facts at the aggregate boundary that owns their invariants.
- Simulated hacking changes only explicitly authorized in-game state. It never grants access to real infrastructure, operating-system accounts, or application credentials.
- Fictional encryption and challenge difficulty are game mechanics, not real security guarantees.
- Read models used for display, search, printing, or manager workflows are derived and rebuildable. The management client sends commands through the backend; it never edits the event store directly.

## Required verification

The test suites must include:

- Aggregate tests proving valid commands emit the expected events and invalid transitions emit none.
- Optimistic-concurrency tests, per feature, proving that two commands based on one stream version cannot both commit.
- Projection tests proving a clean rebuild from history yields the same query state as incremental processing, including duplicate delivery and failure/recovery.
- Upcaster tests for each registered upcaster, plus replay of stored historical payload fixtures.
- Tests that corrections append compensating facts and do not mutate prior history.

These tests verify this application's aggregates, projections, and Marten configuration; they do not re-test Marten's own storage guarantees. Aggregate and feature-invariant tests are added with their domain capabilities; this avoids introducing placeholder game behavior before its feature specification is reached.
