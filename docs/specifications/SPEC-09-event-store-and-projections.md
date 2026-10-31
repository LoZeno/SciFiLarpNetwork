# SPEC-09: Event store and projections

## Requirement and selected direction

Use RavenDB as the initial datastore, with event records as the durable source of truth and RavenDB indexes/projections (including map/reduce where appropriate) for queryable current-state snapshots. This selects the RavenDB option from the requested alternatives; do not add a second datastore or use mutable read models as the event log.

## Acceptance criteria

- Store event records with stream/aggregate identity, sequence/version, event type/schema version, timestamp, and payload sufficient to replay state.
- Use optimistic concurrency or an equivalent atomic append mechanism to prevent silently overwriting concurrent events.
- Implement projections as rebuildable indexes/read models. Document whether each projection is immediately or eventually consistent and how clients observe its freshness.
- Demonstrate datastore operation and packaging compatibility on the requested OS/architecture matrix before relying on it for application distribution.
- Document licensing, deployment mode, backup/restore, upgrades, and local development setup before production use.
- If RavenDB cannot satisfy the verified platform, licensing, or deployment constraints, stop and present the tradeoff for approval before switching to PostgreSQL/Marten.
