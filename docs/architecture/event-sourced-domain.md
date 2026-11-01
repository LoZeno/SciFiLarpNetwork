# Event-sourced domain

## Authority and responsibilities

The append-only event history is the authoritative record of accepted domain changes. A projection, snapshot, cache, or user-interface view is derived state: it may be discarded and rebuilt, and it must never be treated as the only surviving record of a change.

The write path is:

1. A client submits a command expressing intent, not a pre-built event or a replacement entity document.
2. The application layer authenticates the actor, checks authorization and command-level input constraints, and loads the target aggregate from its ordered event stream.
3. The aggregate decides whether the requested transition is valid. It emits zero or more immutable domain events; invalid transitions emit no events.
4. The event-store boundary appends the batch atomically only if the stream is still at the version the aggregate read. A conflict rejects the command. The caller reloads and re-evaluates the intent; it must not blindly retry stale events.
5. Projections consume committed events in stream order and expose rebuildable query models.

Transport, authentication, persistence-provider APIs, and projection storage do not belong in domain aggregates. The RavenDB-specific adapter, durable append implementation, and runtime projection workers are introduced under SPEC-09; this document defines their contract, not a substitute in-memory production store.

## Commands, streams, and transaction boundaries

- Commands are explicit requests such as `RegisterCharacterForEvent`; they are not persisted and are not trusted as proof that an operation succeeded.
- An aggregate is the consistency boundary for a set of invariants that must be decided atomically. Keep aggregates small enough to avoid unrelated write conflicts. Do not prescribe an aggregate per database document or combine all LARP data into one aggregate.
- Each aggregate instance has one stable stream identifier and a monotonically increasing stream version. Sequence numbers start at 1 and are contiguous; a stream's expected version is 0 when it has no events.
- A successful command appends its complete event batch to one stream in one atomic operation. A rejected or conflicting append leaves the stream unchanged.
- Cross-aggregate work is not an implicit transaction. If a capability needs multiple streams, make its consistency model and failure recovery explicit; use an idempotent process/workflow and compensating domain events where appropriate.
- Retrying a command after a concurrency conflict means reloading state and rerunning authorization and domain decisions. Event IDs or command idempotency keys prevent a retried request from applying the same accepted intent twice.

Aggregate boundaries are chosen with each capability's invariants, not fixed prematurely for all features. Candidate areas include Account, Player, Character, Network, configurable Group and Skill catalogs, Event and its days/participation, messages and per-character mailbox state, news stories, personal files/datapads, and item/substance catalogs. Their final boundaries are established alongside the corresponding feature specifications. Cross-references use stable identifiers; they do not merge the glossary's distinct Account, Player, and Character concepts.

## Event contract and history integrity

Every stored event has:

| Metadata | Meaning |
|---|---|
| Event ID | Globally unique identity for deduplication and audit. |
| Stream ID and sequence | Aggregate identity and contiguous ordering within that stream. |
| Event type and schema version | Stable, explicit event name and positive version; neither depends on a CLR type name. |
| Occurred/recorded time | UTC instants; distinguish the domain occurrence time from store acceptance time where both matter. |
| Actor, correlation, and causation IDs | Auditable initiator and links between commands/events, nullable only for explicitly system-originated work. |
| Payload | The immutable facts needed to replay the accepted transition, without transport or projection-only fields. |

Events are append-only through normal application operations. Corrections, reversals, administrative changes, and fictional breach effects are new, auditable events; they do not edit or delete earlier facts. Event payloads must not contain plaintext passwords, session/bearer tokens, QR identity secrets, or other reusable authentication secrets. Authentication verifier storage and retention must be designed with the authentication specification rather than copied casually into projections or audit metadata.

## Replay, schema evolution, and snapshots

- Replay applies events in ascending stream sequence and validates the expected next sequence. A gap, duplicate, wrong stream, unknown event type, or unsupported schema version is a surfaced integrity error; it is not silently skipped.
- Event type names and schema versions are explicit persisted contracts. A new incompatible payload shape increments its schema version.
- Upcasters are deterministic, pure transformations from one stored schema version to the next supported in-memory version. They do not rewrite stored history. Keep representative historical payload fixtures so supported versions remain replayable.
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

- Ordered append/replay tests that verify contiguous sequence numbers, stream identity, and rejection of gaps or duplicates.
- Aggregate tests proving valid commands emit the expected events and invalid transitions emit none.
- Optimistic-concurrency tests proving that two writes based on one version cannot both silently commit.
- Projection tests proving a clean rebuild from history yields the same query state as incremental processing, including duplicate delivery and failure/recovery.
- Schema evolution tests for each supported event version and upcaster path, plus deterministic replay from stored fixtures.
- Tests that corrections append compensating facts and do not mutate prior history.

The store-level and projection-worker tests depend on the durable provider contract and are implemented with SPEC-09. Aggregate and feature-invariant tests are added with their domain capabilities; this avoids introducing placeholder game behavior before its feature specification is reached.
