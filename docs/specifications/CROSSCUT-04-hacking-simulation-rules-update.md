# Cross-cutting specification: Hacking interaction rules update

## Purpose and prerequisites

Update the reusable theatrical hacking component and the local demonstration page delivered by [CROSSCUT-02](CROSSCUT-02-cinematic-hacking.md) to the rules in [SPEC-26](SPEC-26-hacking-game-settings.md). CROSSCUT-02 stays the reference for the safety boundary, the inert code display, focus, accessibility, and localization. Where this specification and CROSSCUT-02 disagree about the demonstration profile or penalty handling, this specification governs.

Implement this after SPEC-26 and before [SPEC-27](SPEC-27-hacking-plugs.md) and [CROSSCUT-03](CROSSCUT-03-server-authoritative-hacking-challenges.md). It changes the frontend only. The component stays callback-driven and decides no authorization, difficulty, success, or consequence.

## Component changes

- **Any number of penalty characters.** Remove the requirement that a progress update has exactly four distinct penalty characters. Accept zero or more distinct, printable characters. The component never renders them outside the demo-only view.
- **Keystroke cap.** The challenge view carries `maxKeystrokesPerSecond`. The component does not forward a key to the attempt callback, and does not advance the inert code stream, when the key would exceed the cap. Keyboard and keypad input are limited the same way, and held-key repeats are still ignored. A short, localized, accessible cue (for example “Input too fast”) is shown while keys are being dropped.
- **Server throttling.** The attempt callback may report that an attempt was throttled. The component then shows the same cue, does not change progress, and does not treat it as an error. The server remains the authority on the cap.
- **Rotation after every attempt.** The target and penalty characters change after every accepted attempt. A displayed hint is cleared after every accepted attempt, because it no longer refers to the current target.
- **Duration.** The component displays the deadline it is given. It does not assume 60 seconds.
- **Guarantee plugs.** A challenge that the server resolves as an immediate success is displayed as completed without requiring typing. Whether a brief non-interactive sequence is shown is decided with the consuming feature.

## Demonstration page

The page stays unlinked and direct-route-only (`/theatrical-hacking-test`), local-only, and unable to grant access or call the backend.

- Inputs: difficulty (whole number from 1, no maximum), effective hacking level (decimal from 0, no maximum), number of penalty characters (default 0), keystroke cap (default 3), and the four formula settings of SPEC-26 with their defaults (hits base, hits per difficulty, duration base seconds, duration seconds per level).
- The “minimum hits” input of CROSSCUT-02 is removed.
- The page computes required hits and duration with the SPEC-26 formulas, using the published test vectors in its tests. A level below 1 does not start a challenge and shows a localized message.
- The target and penalty characters rotate after every keystroke. Penalty characters and the hint control are shown only on this page, as before.
- The page enforces the keystroke cap in its local simulation.

## Acceptance criteria

- The component accepts challenge views and results with zero, one, or many penalty characters, and rejects duplicate or non-printable penalty characters.
- Input faster than the cap is dropped without calling the attempt callback, advancing the code stream, or changing progress, and the cue is announced to assistive technology.
- A throttled result from the callback produces the cue and no error state.
- Hints, penalty lists, and demo controls remain absent from the reusable production presentation.
- The demonstration page's formulas match the SPEC-26 test vectors, level 0 and below cannot start, and difficulty, level, penalties, and cap have no upper limit in the code.
- The apostrophe handling, focus behavior, keypad, timer, accessibility, localization, and inert-code requirements of CROSSCUT-02 still pass.

## Verification

- Update the existing component and demonstration page tests. Replace the tests that assume exactly four penalties, a 60-second duration, penalty rotation only on progress changes, and the minimum-hits input.
- Add tests for the keystroke cap (keyboard and keypad), the throttle cue, any number of penalties, rotation and hint clearing after each attempt, the formula test vectors, and the level-below-1 refusal.
- Keep the TypeScript coverage gate, localized strings in English and Italian, and all existing accessibility checks.
