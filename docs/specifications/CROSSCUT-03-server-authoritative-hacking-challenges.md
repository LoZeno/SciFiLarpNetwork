# Cross-cutting specification: Server-authoritative hacking challenges

## Purpose and dependencies

Provide the durable, server-authoritative challenge workflow consumed by the fictional interaction in [CROSSCUT-02](CROSSCUT-02-cinematic-hacking.md) and by the Phase G Web Forms-derived features. This specification owns challenge rules and lifecycle; each consuming feature still owns its target eligibility, permitted outcome, and any scoped in-game access it grants.

Implement this after the character, skill, event/network, authentication, and durable event-store capabilities required by it are available, and before WEBFORM-16 starts a production challenge. Implement [SPEC-26](SPEC-26-hacking-game-settings.md), [CROSSCUT-04](CROSSCUT-04-hacking-simulation-rules-update.md), and [SPEC-27](SPEC-27-hacking-plugs.md) first; they define the rules, settings, client contract, and plugs applied here. Use the ASP.NET Core MVC/Siren API and PostgreSQL/Marten event store. Do not introduce placeholder player, character, network-account, or authentication models solely to bypass those dependencies.

The challenge is a LARP mechanic, not a real security boundary. It must not be used to protect real infrastructure, application credentials, or operating-system resources.

## Challenge creation and authoritative rules

- Challenge creation requires an authenticated character and a consuming feature's authorized command. Revalidate the actor, target, network/event scope, target eligibility, current protection, actor skill, relevant game configuration, cooldown, and any configured in-game override at command time. Never accept these authoritative values from the client.
- Issue an unpredictable challenge ID and server-side secret target character from a configured alphabet, together with the authoritative deadline and required-hit count. Do not reveal the target in the ordinary challenge representation, URLs, logs, or client-visible error details.
- Resolve the difficulty from the consuming target/category, the actor's hacking level, any applicable plug ([SPEC-27](SPEC-27-hacking-plugs.md)), and the active game settings on the server, and copy those settings into the initiation event. Derive the effective hacking level, required hits, and duration with the rules of [SPEC-26](SPEC-26-hacking-game-settings.md):

  `requiredHits = max(1, requiredHitsBase + requiredHitsPerDifficulty × difficulty)` and `durationSeconds = min(3600, round(durationBaseSeconds + durationSecondsPerLevel × effectiveLevel))`

- A hacking level below 1 cannot start a challenge, with or without a plug. Difficulty and levels have no maximum in code. Clamp the required count to at least one. The demonstration page's formula inputs are not production values; the SPEC-26 settings own them.
- Store all values required to validate and replay challenge decisions in the authoritative challenge stream or other explicitly specified server-side event-sourced state. Client-submitted difficulty, skill, deadline, progress, target key, outcome, and event payloads are never accepted as authority.
- A challenge has a single authenticated actor, target/scope, immutable initial rules/deadline, monotonically versioned progress, and one terminal outcome. Duration, penalty count, and keystroke cap come from the SPEC-26 settings copied into the initiation event; the allowed alphabet, cooldowns, and override rules remain game policy.

## Attempts, lifecycle, and access outcomes

- Each accepted input is submitted to the server with the challenge ID and the challenge version the client last observed. The server loads the challenge with `FetchForWriting` at that expected version, verifies ownership by the authenticated actor, active status, and deadline, and normalizes and validates the input against the server-side challenge state.
- A valid target hit increments progress. When the settings configure N penalty characters, they are N distinct characters different from the target, and pressing one reduces progress by one, clamped at zero. After every accepted attempt, whether a hit, a penalty, or an ordinary key, the server draws a new target and new penalty characters from a cryptographically secure random source and records them in the attempt event. Ordinary incorrect input does not change progress.
- Client key-repeat suppression and keypad behavior are usability controls, not trust boundaries. Duplicate delivery of the same attempt cannot count twice: the repeat carries an expected version the first delivery already advanced, so Marten rejects it with `ConcurrencyException`. Concurrent attempts are rejected the same way, so stale writes cannot silently overwrite newer progress.
- Apply the SPEC-26 keystroke cap (default 3 per second) server-side per authenticated actor and challenge, and, separately, flood limits above it that bound abusive traffic. A throttled request returns a stable rate-limit response with retry guidance and does not count as an attempt, change progress, or trigger game cooldowns/consequences. Rate limits must not depend on client-side debouncing.
- The server deadline is authoritative. An attempt arriving at or after expiry cannot succeed. Resolve expiry into durable terminal state even if the player disconnects, by an expiry worker or an equivalent server-owned lifecycle mechanism. Client timers and animations cannot extend a challenge.
- Determine success, failure, and expiry on the server and persist each terminal transition at most once. Replaying a completed challenge or submitting after a terminal state cannot produce another success or another consequence. Retrying an accepted command returns a stable conflict, not a second application; the client then reads the current challenge status.
- Emit auditable events for challenge initiation and accepted progress changes, and exactly one terminal success/failure/expiry event as applicable. Record actor, target/scope, time, challenge/correlation IDs, outcome, and relevant policy/version metadata; do not put reusable authentication secrets in event payloads.
- Apply configured cooldown, alert, and game consequences from server-side policy. Failed or expired challenges never disclose protected target data.
- A successful challenge is evidence only for the consuming feature's explicitly scoped in-game decision. It does not grant generic account access. The consumer revalidates authorization when creating any breach context and on every subsequent read/edit/copy action. When the command that records challenge success also decides the consuming feature's scoped access grant, append both in the same Marten session so they commit atomically. When the grant is a later, separate step, make it a recoverable process keyed by the challenge ID that rejects a repeat (see the retry rules in [the event-sourced domain architecture](../architecture/event-sourced-domain.md)).

## API and presentation contract

- Expose challenge creation, attempt submission, status/result, and any permitted accessibility hint as typed MVC/Siren actions with stable machine-readable errors for unauthorized, ineligible, expired, completed, cooldown, validation, and concurrency outcomes.
- Every request independently authenticates and authorizes the actor and target. Siren links/actions are discoverability only, never security checks.
- Return only the challenge state needed by the client: opaque challenge ID, deadline, required/current progress, and outcome. Keep secret target and any hidden penalty values server-side unless an explicitly defined accessible mode requires a controlled disclosure.
- Ensure authorized accessibility support (for example a game-configured hint or alternate input mode) cannot be confused with ordinary challenge state. The consuming UI makes the available mode clear and does not accidentally expose demo-only controls or hidden penalty lists.
- Use generic, configurable network and game terminology. All code-like display text remains the inert CROSSCUT-02 presentation and is never part of command execution or challenge validation.

## Event sourcing and consistency

- Model challenge transitions as domain decisions over ordered events. Command handlers follow [SPEC-24](SPEC-24-event-store-runtime.md): load the challenge aggregate with `FetchForWriting`, append the resulting events, and commit with one `SaveChangesAsync`. Do not substitute an in-memory production store or mutable current-state-only table.
- Register every challenge event type with Marten so stored event type names are stable; evolve payloads with Marten's event type mapping and upcasting, never by rewriting stored events. Use UTC timestamps, actor/correlation/causation metadata, and immutable payloads compatible with `docs/architecture/event-sourced-domain.md`.
- A retried or concurrent initiation must not create parallel active challenges. Enforce this through `FetchForWriting` on the aggregate that owns the "one active challenge" invariant, so the second initiation fails with a concurrency conflict; the challenge stream itself keeps its unpredictable ID. Make attempts and terminal transitions replay-safe. Define how active challenges and cooldowns are reconstructed after restart and how event projections recover.
- Link challenge success and any consuming feature's breach/access grant by stable IDs. Recheck feature authorization at the point where access is granted and used.

## Acceptance criteria and tests

- Formula tests use the SPEC-26 test vectors and cover difficulty and level boundaries, very high levels and difficulties, the level-below-1 refusal, plug effects from SPEC-27, and the invariant that required hits never fall below one.
- Domain tests prove valid creation and attempts emit the expected events, invalid authorization/target/cooldown/state emits none, correct hits change progress, ordinary incorrect input does not, and configured penalties clamp progress and the target and penalties rotate after every accepted attempt.
- Rate-limit tests prove input at or below the configured cap is accepted, excessive bursts are throttled server-side, and throttled requests do not mutate challenge progress or trigger game consequences.
- API tests prove a client cannot override actor, target, difficulty, skill, required hits, progress, secret key, deadline, or result; all challenge actions reject unauthenticated or mismatched actors and revalidate target eligibility.
- Persistence/integration tests prove expected-version conflict handling for concurrent attempts, rejection of duplicate request delivery, rejection of a parallel initiation, one-time terminal events/consequences, atomic commit of success and any same-command access grant, expiry after disconnect/restart, and recovery of active challenge/cooldown state from events.
- Tests cover target secrecy in normal responses/logs/errors, accessibility mode authorization, and safe rendering of inert code regardless of submitted input.
- Tests verify challenge success alone does not grant unscoped access, and that consuming workflows reauthorize, scope, and reject repeats of any resulting in-game access.
- Retain the repository's .NET and frontend coverage gates, xUnit backend tests, localization, and offline operation requirements.
