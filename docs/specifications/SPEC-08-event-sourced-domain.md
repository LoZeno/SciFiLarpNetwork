# SPEC-08: Event-sourced domain

## Requirement

The backend persists domain changes as events rather than treating mutable current-state tables/documents as the source of truth. Current-state data is derived into queryable projections/snapshots.

## Acceptance criteria

- Domain commands validate invariants and append events to an identified stream with ordering/concurrency protection.
- Events are durable, versioned, and immutable through normal application operations; corrections use explicit compensating events rather than rewriting history.
- Read models/projections can be rebuilt from the event history and are not the authoritative record.
- Event schemas have a documented evolution/upcasting approach, and projection failures can be detected and recovered without losing committed events.
- The architecture names the required event-ordering, invalid-transition, concurrency-conflict, and projection-rebuild tests; store/projection tests accompany the selected provider's backend implementation, and aggregate-invariant tests accompany the relevant feature work.

## Architecture baseline

The provider-neutral command, aggregate, stream, event metadata, schema evolution, projection freshness/recovery, and test contracts are defined in [event-sourced domain](../architecture/event-sourced-domain.md). SPEC-09 selects PostgreSQL + Marten and defines the provider contract; backend implementation and its integration tests follow with backend work. Feature specifications introduce their own aggregate behavior and tests. This task intentionally does not add a competing in-memory production store, provider dependency, or placeholder game entities.
