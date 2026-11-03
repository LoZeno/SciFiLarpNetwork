# Cross-cutting specification: Theatrical hacking interaction

## Purpose and safety boundary

Provide a reusable, cinematic interface for an explicitly fictional, in-game challenge. The screen may show only prewritten inert code-like text; it must never execute, compile, interpret, or transmit that text, perform network probing, collect real credentials, or imply access to real systems. The display is presentation only and is never authoritative for game outcomes.

The reusable React control accepts challenge state and callbacks for attempts, hints, and expiry. It does not decide authorization, challenge difficulty, success, cooldowns, or game consequences. Production challenge creation and validation are specified separately in [CROSSCUT-03](CROSSCUT-03-server-authoritative-hacking-challenges.md).

## Interaction requirements

- Show a fixed-height terminal with an internal scrollbar. In the demo, each accepted input advances one character of a long inert Rust-styled snippet, which loops at its end.
- Automatically focus the terminal when an active challenge starts. Printable input is handled only when the terminal itself has focus; ignore modifier combinations, repeated keydown events, and input from unrelated controls. Prevent browser defaults for supported input keys, including apostrophe, so browser shortcuts such as Firefox Quick Find do not steal focus.
- Do not insert user input into the displayed code. Any input that advances the visual stream is separate from the character submitted to the challenge callback.
- Display a timer and accessible progress indicator. Stop accepting input after the challenge is no longer active or its deadline has elapsed; the server remains the source of the deadline and outcome.
- Provide keyboard and touch/keypad input, visible focus, screen-reader labels/status, and reduced-motion behavior. Production integrations must offer an accessible way to discover or enter the target without exposing it in the ordinary challenge response.
- In the reusable production presentation, do not render penalty-character lists, demo hints, or demo controls. The challenge workflow may expose an explicitly authorized accessible hint mode when specified by its consuming feature.
- Show localized, clear active, progress, success, expiry, and error states. The success state says “ACCESS GRANTED” (localized). Visual styling and wording must identify fictional gameplay rather than real intrusion.

## Local demonstration profile

- Provide the unlinked, direct-route-only `/theatrical-hacking-test` page. It lets a tester set difficulty (1–10), minimum hits (default 1), and attacker hacking level, then starts a 60-second **local-only** simulation.
- The demo uses difficulty itself as `baseHits(difficulty)` and calculates:

  `requiredHits = max(1, minimumHits + baseHits(difficulty) - max(0, attackerHackingLevel))`

  The minimum is at least one. Production configuration and calculation are server-authoritative under CROSSCUT-03.
- Select a target character randomly from the configured demo alphabet. A correct hit advances progress; after a non-final correct hit, select a different target character. Other ordinary keys do not advance or reduce challenge progress.
- Select four distinct demo penalty characters, each different from the current target. Hitting one reduces progress by one, clamped at zero. Whenever progress moves forward or backward, replace the four penalty characters with four new distinct characters. Penalty characters and the “Request a hint” control/result are visible only on this demo page.
- Each input advances the inert visual code stream, independently of whether it is a target, penalty, or ordinary character. The code stream never affects the local result.
- The route does not call the backend, grant or record game access, or prove that server authorization, durable event persistence, replay protection, or concurrency handling exists. Its target, progress, timer, and outcomes are demonstration state only.

## Acceptance criteria

- The reusable component is callback-driven and can be mounted without the demo-only controls or penalty/hint display.
- Keyboard, keypad, focus, progress, deadline display, success/expiry states, and callback errors are tested.
- The apostrophe regression test verifies browser-default prevention and that focus remains on the terminal.
- Demo tests cover the hit formula and lower bound, target changes, four distinct penalties and their rotation, progress clamping, timer expiry, and local-only behavior.
- Tests verify user input is never interpolated into the inert code and all displayed game text is localized.
