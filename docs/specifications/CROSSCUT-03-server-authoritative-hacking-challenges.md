# Cross-cutting specification: Server-authoritative hacking challenges

## Purpose and dependencies

Provide the durable, server-authoritative challenge workflow consumed by the fictional interaction in [CROSSCUT-02](CROSSCUT-02-cinematic-hacking.md) and by the Phase G Web Forms-derived features. This specification owns challenge rules and lifecycle; each consuming feature still owns its target eligibility, permitted outcome, and any scoped in-game access it grants.

Implement this after the character, skill, event/network, authentication, and durable event-store capabilities required by it are available, and before WEBFORM-16 starts a production challenge. Use the ASP.NET Core MVC/Siren API and PostgreSQL/Marten event store. Do not introduce placeholder player, character, network-account, or authentication models solely to bypass those dependencies.

The challenge is a LARP mechanic, not a real security boundary. It must not be used to protect real infrastructure, application credentials, or operating-system resources.

## Challenge creation and authoritative rules

- Challenge creation requires an authenticated character and a consuming feature's authorized command. Revalidate the actor, target, network/event scope, target eligibility, current protection, actor skill, relevant game configuration, cooldown, and any configured in-game override at command time. Never accept these authoritative values from the client.
- Issue an unpredictable challenge ID and server-side secret target character from a configured alphabet, together with the authoritative deadline and required-hit count. Do not reveal the target in the ordinary challenge representation, URLs, logs, or client-visible error details.
- Resolve difficulty and `baseHits(difficulty)` from the consuming target/category and active game configuration. Resolve the configured minimum (at least one) and the actor's current hacking level on the server. Apply this exact formula:

  `requiredHits = max(1, minimumHits + baseHits(difficulty) - max(0, attackerHackingLevel))`

- Treat negative hacking levels as zero for this calculation. Clamp the final required count to at least one regardless of skill. The demo's identity mapping from difficulty to base hits is not a production default; game configuration owns that mapping.
- Store all values required to validate and replay challenge decisions in the authoritative challenge stream or other explicitly specified server-side event-sourced state. Client-submitted difficulty, skill, deadline, progress, target key, outcome, and event payloads are never accepted as authority.
- A challenge has a single authenticated actor, target/scope, immutable initial rules/deadline, monotonically versioned progress, and one terminal outcome. Define configured challenge duration, allowed alphabet, hit/penalty behavior, cooldowns, and any override rules as game policy.

## Attempts, lifecycle, and access outcomes

- Each accepted input is submitted to the server with the challenge ID and an idempotency identifier. The server verifies ownership by the authenticated actor, active status, deadline, and expected challenge version; it normalizes and validates the input against the server-side challenge state.
- A valid target hit increments progress. Apply any configured penalty-key rules on the server; if the active policy uses the demo's four penalties, they are distinct from the target, reduce progress by one clamped at zero, and rotate whenever progress changes. Ordinary incorrect input does not change progress.
- Client key-repeat suppression and keypad behavior are usability controls, not trust boundaries. Duplicate delivery of the same attempt identifier cannot count twice; concurrent attempts serialize against the event-stream version so stale writes cannot silently overwrite newer progress.
- Apply server-side attempt rate limits per authenticated actor and challenge, configured to permit the intended keyboard/touch play cadence while bounding request floods. A throttled request returns a stable rate-limit response with retry guidance and does not count as an attempt, change progress, or trigger game cooldowns/consequences. Rate limits must not depend on client-side debouncing.
- The server deadline is authoritative. An attempt arriving at or after expiry cannot succeed. Resolve expiry into durable terminal state even if the player disconnects, by an expiry worker or an equivalent server-owned lifecycle mechanism. Client timers and animations cannot extend a challenge.
- Determine success, failure, and expiry on the server and persist each terminal transition at most once. Replaying a completed challenge or submitting after a terminal state cannot produce another success or another consequence. Retrying an accepted command returns its recorded result or a stable conflict, not a second application.
- Emit auditable events for challenge initiation and accepted progress changes, and exactly one terminal success/failure/expiry event as applicable. Record actor, target/scope, time, challenge/correlation IDs, outcome, and relevant policy/version metadata; do not put reusable authentication secrets in event payloads.
- Apply configured cooldown, alert, and game consequences from server-side policy. Failed or expired challenges never disclose protected target data.
- A successful challenge is evidence only for the consuming feature's explicitly scoped in-game decision. It does not grant generic account access. The consumer revalidates authorization when creating any breach context and on every subsequent read/edit/copy action. Make cross-stream success-to-access handling idempotent and recoverable; do not claim a multi-stream atomic transaction unless the chosen store implements one.

## API and presentation contract

- Expose challenge creation, attempt submission, status/result, and any permitted accessibility hint as typed MVC/Siren actions with stable machine-readable errors for unauthorized, ineligible, expired, completed, cooldown, validation, and concurrency outcomes.
- Every request independently authenticates and authorizes the actor and target. Siren links/actions are discoverability only, never security checks.
- Return only the challenge state needed by the client: opaque challenge ID, deadline, required/current progress, and outcome. Keep secret target and any hidden penalty values server-side unless an explicitly defined accessible mode requires a controlled disclosure.
- Ensure authorized accessibility support (for example a game-configured hint or alternate input mode) cannot be confused with ordinary challenge state. The consuming UI makes the available mode clear and does not accidentally expose demo-only controls or hidden penalty lists.
- Use generic, configurable network and game terminology. All code-like display text remains the inert CROSSCUT-02 presentation and is never part of command execution or challenge validation.

## Event sourcing and consistency

- Model challenge transitions as domain decisions over ordered events; persist through the selected PostgreSQL/Marten adapter with expected-version concurrency. Do not substitute an in-memory production store or mutable current-state-only table.
- Use explicit stable event names/schema versions, UTC timestamps, actor/correlation/causation metadata, and immutable payloads compatible with `docs/architecture/event-sourced-domain.md`.
- Make initiation idempotent where a retried client command must not create parallel active challenges. Make attempts and terminal transitions replay-safe. Define how active challenges and cooldowns are reconstructed after restart and how event projections recover.
- Ensure challenge success and any consuming feature's breach/access grant are linked by stable IDs and idempotent processing. Recheck feature authorization at the point where access is granted and used.

## Acceptance criteria and tests

- Formula tests cover the configured difficulty/base-hit boundaries, minimum values, negative skill, very high skill, and the invariant that total required hits never falls below one. Include the demo example: minimum 1 and base hits 3 require 4 hits at level 0 before any penalties.
- Domain tests prove valid creation and attempts emit the expected events, invalid authorization/target/cooldown/state emits none, correct hits change progress, ordinary incorrect input does not, and configured penalties clamp/rotate as specified.
- Rate-limit tests prove ordinary intended input cadence is accepted, excessive bursts are throttled server-side, and throttled requests do not mutate challenge progress or trigger game consequences.
- API tests prove a client cannot override actor, target, difficulty, skill, required hits, progress, secret key, deadline, or result; all challenge actions reject unauthenticated or mismatched actors and revalidate target eligibility.
- Persistence/integration tests prove ordered replay, expected-version conflict handling for concurrent attempts, idempotent duplicate request delivery, one-time terminal events/consequences, expiry after disconnect/restart, and recovery of active challenge/cooldown state from events.
- Tests cover target secrecy in normal responses/logs/errors, accessibility mode authorization, and safe rendering of inert code regardless of submitted input.
- Tests verify challenge success alone does not grant unscoped access, and that consuming workflows reauthorize, scope, and deduplicate any resulting in-game access.
- Retain the repository's .NET and frontend coverage gates, xUnit backend tests, localization, and offline operation requirements.
