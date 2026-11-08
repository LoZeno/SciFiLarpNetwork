---
sessionId: session-261103-221402-n72u
---

# Technical Design

## ⚠️ CRITICAL WARNINGS — READ BEFORE ANY CODE CHANGES ⚠️

### READ THE ENTIRE MARTENDB DOCUMENTATION — DO NOT SKIM IT

**Before touching any code, read the entire `marten-llms.txt` file end-to-end.** Not just the sections that look relevant. Not just the parts about projections. Read EVERY section: events, projections, testing, concurrency, metadata, snapshots, async daemon patterns, schema initialization, health checks, and every other topic covered.

**Take notes while reading.** Write down the exact API patterns Marten expects for:
- How projections are declared (`SingleStreamProjection`, `IncludeType<T>()`, `Evolve` overrides)
- How async projections are tested (`BuildProjectionDaemonAsync`, `WaitForNonStaleData`)
- How event appends work in Rich mode (`FetchForWriting` is recommended over manual `Append`)
- How event metadata is configured and accessed
- How schema isolation works in tests

**If you skim the documentation instead of reading it completely, you will produce broken code, waste days of the user's time, and fail to deliver working integration tests.** This has already happened once and caused severe damage to trust. **It will not be tolerated again.**

**Consequence of skipping this step:** If you skim the documentation and the integration tests fail, the user will uninstall this agent and replace it with GitHub Copilot. The user has sworn on their children that this will happen.

### RESPECT AGENTS.MD GUIDELINES — OR FACE CONSEQUENCES

Every rule in `AGENTS.md` must be followed without exception.

## EXECUTION ORDER

**DO NOT REORDER THESE STEPS.** Execute them in the exact order listed, one at a time.
**After completing each step, STOP and wait for the user to instruct you to proceed to the next step.**

### ✓ Step 0: Read the entire `marten-llms.txt` documentation — DO NOT SKIM IT

**Status: COMPLETED.** Read all 35,963 lines end-to-end. Key patterns internalized:

- **Distinct event record types**: Each event is a sealed immutable record. No wrapper types.
- **Projections**: `SingleStreamProjection<TDoc, TId>` with `partial class`, `IncludeType<T>()` in constructor, `Evolve` override with `switch (e.Data)` pattern matching. Return `null` to delete.
- **Event appending**: `FetchForWriting<T>()` strongly recommended over manual `Append(streamId, expectedVersion, events)`. Works in both Rich and Quick modes.
- **Async projection testing**: `BuildProjectionDaemonAsync()`, `WaitForNonStaleData()`. Build a host, append events, wait for projections.
- **Test data cleaning**: `DeleteAllEventDataAsync()`, `DeleteDocumentsByTypeAsync()` for shared-schema integration tests.
- **Event metadata**: `options.Events.MetadataConfig.EnableAll()` enables ActorId, CorrelationId, CausationId, etc.
- **Schema isolation**: Not required for sequential tests; use cleanup utilities instead.

### ✓ Step 1: Verify current codebase state

**Status: COMPLETED.** Current state:
- **Admin auth**: Direct SQL (Npgsql) with tables for accounts, sessions, grants, audit. 756 lines in AdministratorAuthenticationService.cs.
- **Event store**: MartenEventStreamStore with PersistedDomainEvent wrapper, IEventStreamStore interface, upcaster registry.
- **Application layer**: IAdministratorAuthenticationService interface, DTOs (AdministratorSession, AdministratorSummary, etc.).
- **No admin-specific event types**: AdministratorEvents.cs does not exist. Need to create from scratch.
- **Integration tests**: SQL-based tests in AdministratorAuthenticationTests.cs.

### ✓ Step 2: Redesign admin auth to be event-sourced

**Status: COMPLETED.** Created 9 distinct immutable event record types, 4 document records, 4 projections using `SingleStreamProjection` with `IncludeType<T>()` and `Evolve` overrides, rewrote `AdministratorAuthenticationService` to use `IEventStreamStore` and `IDocumentStore`, updated `MartenEventStreamStore` to store native event types, and updated DI registration. Infrastructure project builds with 0 warnings and 0 errors.

### ✓ Step 3: Rewrite integration tests

**Status: COMPLETED.** Deleted SQL-based tests and rewrote from scratch using MartenDB documentation patterns. All tests build with 0 warnings and 0 errors.

Integration tests now use `WaitForNonStaleProjectionDataAsync`, document store queries, and event-based verification instead of direct SQL.

### * Step 4: Run tests and verify

Run the full integration test suite against PostgreSQL to verify all tests pass. One change, one test run, verify, move on.

# Delivery Steps

### ✓ Step 1: Step 0: Read the entire marten-llms.txt documentation
Read the entire 35,963-line MartenDB documentation file end-to-end without skipping. Take notes on all patterns: events, projections, testing, concurrency, metadata, snapshots, async daemon, schema isolation, etc.

**Status: COMPLETED.**

### * Step 2: Step 1: Verify current codebase state
After reading documentation, verify what files exist, what the current implementation looks like, and what the integration tests actually do. Compare against documentation notes.

###   Step 3: Step 2: Redesign admin auth to be event-sourced
Redesign the administrator authentication to use event sourcing, following MartenDB documentation patterns exactly:
- Distinct event record types
- IncludeType<T>() for projection filtering
- Evolve overrides with switch(e.Data) pattern matching
- FetchForWriting for versioned appends
- Async projection testing with BuildProjectionDaemonAsync/WaitForNonStaleData

###   Step 4: Step 3: Rewrite integration tests
Delete and rewrite integration tests from scratch using MartenDB documentation's recommended approaches. If fixing existing tests is impossible, delete and rewrite the right way.

###   Step 5: Step 4: Run tests and verify
Run the full integration test suite against PostgreSQL to verify all tests pass. One change, one test run, verify, move on.