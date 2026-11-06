---
sessionId: session-261103-221402-n72u
---

# Technical Design

### Current Implementation

The administrator authentication was migrated from direct SQL to event sourcing with MartenDB. Events are stored as `PersistedDomainEvent` (a custom record with `EventType` string and `Payload` as JsonElement). Four projections project events to read documents:

1. **AdministratorAccountProjection** — projects account lifecycle events
2. **AdministratorSessionProjection** — projects session events, with `ShouldDelete` for revocations
3. **AdministratorGrantProjection** — projects bootstrap/recovery credential grants
4. **AdministratorAuditProjection** — projects audit events

All projections use `static` methods (`Create`, `Apply*`, `ShouldDelete`) with `PersistedDomainEvent` parameters. The authentication service reads from these projections via `IDocumentStore`.

### Issues Identified

**Issue 1: Projections use static methods Marten can't find**

Marten 9's source generator looks for `public` instance methods (`Create`, `Apply`, `ShouldDelete`) on `partial` classes. Our projections use `static` methods with `PersistedDomainEvent` parameters, which the generator ignores. Result: projections never execute, documents are never created, and sign-in fails with HTTP 500.

**Fix:** Override `Evolve(snapshot, id, IEvent e)` in each projection, casting `e.Data` to `PersistedDomainEvent` and switching on `EventType`.

**Issue 2: Auth service calls test-only helper**

`AdministratorAuthenticationService` calls `WaitForNonStaleProjectionDataAsync` before every read. This is a Marten test extension, not meant for production. It blocks the service waiting for projections to catch up on every request.

**Fix:** Remove these calls from the service. The async daemon keeps projections current in production.

**Issue 3: Wrong version in Append**

`MartenEventStreamStore.AppendAsync` passes `expectedVersion + events.Count` as the version argument to `session.Events.Append()`. The second argument should be the **expected** version for concurrency validation, not the target version.

**Fix:** Pass `expectedVersion` instead.

### Proposed Changes

**Projections** — each will override `Evolve`:

- **AdministratorAccountProjection**: switch on `account-created` (create), `account-password-changed` (update hash), `account-role-changed` (update role)
- **AdministratorSessionProjection**: switch on `session-created` (create), `session-revoked` (return null to delete)
- **AdministratorGrantProjection**: switch on `bootstrap-credential-issued` / `recovery-credential-issued` (create/update), `grant-consumed` (no-op)
- **AdministratorAuditProjection**: each audit event has its own stream ID (using `EventId`), `Evolve` creates the document

**Authentication Service** — remove `WaitForNonStaleProjectionDataAsync` from `LoadAccountsAsync`, `LoadSessionByTokenHashAsync`, and `LoadSessionsForAccountAsync`.

**MartenEventStreamStore** — fix `AppendAsync` to pass `expectedVersion` (not `expectedVersion + events.Count`) to `session.Events.Append()`.

# Delivery Steps

### ✓ Step 1: Fix all four projections to use Evolve overrides
Rewrite AdministratorAccountProjection, AdministratorSessionProjection, AdministratorGrantProjection, and AdministratorAuditProjection to override `Evolve` instead of using static `Create`/`Apply` methods that Marten's source generator can't find.

- Change each projection from `sealed class` to `partial class`.
- Remove static `Create`, `Apply*`, and `ShouldDelete` methods.
- Add `public override TDoc Evolve(TDoc snapshot, Guid id, IEvent e)` that casts `e.Data` to `PersistedDomainEvent` and switches on `EventType`.
- For `AdministratorSessionProjection`, return `null` from `Evolve` when `session-revoked` is encountered (deletion pattern).
- For `AdministratorGrantProjection`, handle both `bootstrap-credential-issued` and `recovery-credential-issued` as create/update, and `grant-consumed` as a no-op.

### ✓ Step 2: Fix AdministratorAuthenticationService to remove WaitForNonStaleProjectionDataAsync
Remove all calls to `WaitForNonStaleProjectionDataAsync` from the authentication service — this is a test-only extension method, not for production use.

- In `LoadAccountsAsync`, `LoadSessionByTokenHashAsync`, and `LoadSessionsForAccountAsync`: remove the `await documentStore.WaitForNonStaleProjectionDataAsync(...)` calls.
- The service reads from projections synchronously; in production, the async daemon keeps projections up to date.
- Keep `WaitForNonStaleProjectionDataAsync` only in the integration tests where it belongs.

### ✓ Step 3: Fix MartenEventStreamStore.AppendAsync version parameter
Fix the event append call to pass the correct expected version for optimistic concurrency.

- In `MartenEventStreamStore.cs`, change `session.Events.Append(streamId, expectedVersion + events.Count, persistedEvents)` to `session.Events.Append(streamId, expectedVersion, persistedEvents)`.
- The second argument is the **expected** version for concurrency checking, not the new version. Marten validates that the stream's current version matches `expectedVersion` before appending.
- This fix ensures that concurrent appends to the same stream correctly trigger `StreamConcurrencyException`.

### ✓ Step 4: Run integration tests and verify
Run the full administrator authentication integration test suite against PostgreSQL to verify all fixes.

- Run `dotnet test` with the AdministratorAuthentication filter.
- Verify all tests pass, including sign-in, bootstrap, recovery, session management, RBAC, and secret leak checks.
- If tests still fail, diagnose and fix the remaining issues.