# Cross-cutting specification: Theatrical hacking simulation

## Legacy behavior

The shared Web Forms challenge asks for a character number and password, checks the character's hacker level, displays a timer/progress sequence, optionally accepts an override code, and then uses a random success probability influenced by attacker and target levels. Per-target attempt/cooldown behavior is stored for some attack types. Several call paths and attempt-log methods are incomplete or inconsistent.

## Proposed cinematic mini-game

Show an in-character terminal with **prewritten, inert fictional C/C++-styled text** that appears to stream while the player types. Do not execute code, evaluate user input, contact external hosts, or present the simulation as real intrusion. Each attempt receives a server-generated challenge: a secret target character (for example one from an allowed letter/digit set), a base required-hit count derived from the target's configured protection/difficulty, and a deadline. The attacker must press the secret character repeatedly; other keys only advance harmless visual effects and do not count.

Use the explicit count formula:

`requiredHits = max(minimumHits, baseHits(difficulty) - max(0, attackerHackingLevel))`

The configured minimum is at least one. Keep difficulty-to-base-hit mapping and deadline configurable per game. Do not reveal the target key directly in normal mode; provide accessible optional hints or an alternate non-keyboard interaction so the game remains usable. A correct hit updates progress; a wrong key does not reduce required hits. The server validates challenge ID, actor, target, time window, key/hit sequence, and one-time completion. The visual fake-code stream can be deterministic/synthetic and is never an authority for the result.

## Acceptance criteria

- Challenge initiation verifies the authenticated character, target eligibility, target protection, cooldown, and any in-game override item on the server before issuing challenge state.
- Each attempt gets an unpredictable secret key and single-use challenge ID; the client cannot change required hits, attacker level, target difficulty, deadline, or result.
- Required correct presses follow the formula above, clamp to a configured minimum of at least one, and are tested at low/negative/very high skill values and difficulty boundaries.
- Only correct presses count. Wrong keys, paste, autofill, repeat events, keydown/keyup duplication, and input from unrelated controls cannot generate extra hits; key-repeat behavior is explicitly controlled.
- A visible progress indicator and simulated code animation respond to user input without executing, compiling, interpreting, or sending the displayed text anywhere. Input content is not inserted into code in an executable or unsafe way.
- Success/failure is computed and persisted once server-side; retries, replayed requests, tab duplication, disconnect/reconnect, expiry, and concurrent requests cannot award duplicate success.
- Failure and success emit auditable domain events and apply the configured cooldown/alert/game consequences. UI animation does not extend or bypass the authoritative deadline.
- Provide keyboard, touch, screen-reader, reduced-motion, and non-keyboard/accessibility alternatives; ensure the random secret target is discoverable through permitted hints without making the challenge inaccessible.
- All wording and visuals clearly indicate fictional gameplay. There is no shell, process execution, real credential capture, network probing, or actual access-control bypass.
- Tests cover challenge generation, formula/clamping, hit validation, expiry, replay/concurrency, authorization, accessibility mode, and rendering safety.

## Frontend demonstration profile

- The frontend provides an unlinked, direct-route-only demonstration at `/theatrical-hacking-test`. It lets a tester set difficulty (1–10), minimum hits (default 1), and attacker hacking level, then starts a 60-second local simulation. For this demonstration, difficulty itself is the base-hit count; production game-specific difficulty rules must be configured and enforced by the server.
- The local demonstration displays inert Rust-styled fictional code one character per accepted key/keypad input in a fixed-height, internally scrollable terminal. The displayed snippet loops after its final character. After each correct non-final hit, the local demonstration selects a different target character; production target generation and rotation remain server-authoritative.
- The local demonstration also shows four distinct penalty characters, each different from the current target. Hitting one reduces progress by one step, clamped at zero. Whenever progress moves forward or backward, all four penalty characters are replaced with a fresh distinct set. Production penalty assignment and validation remain server-authoritative.
- The test route is not connected to the backend and cannot grant or record game access. The secret and progress are local demonstration state only; it is not evidence that the server-authoritative challenge, authorization, event persistence, replay protection, or concurrency acceptance criteria are implemented.
- The reusable display receives key-attempt and hint callbacks so later game workflows can delegate those operations to the authoritative backend. The test route supplies explicitly local-only callbacks.
