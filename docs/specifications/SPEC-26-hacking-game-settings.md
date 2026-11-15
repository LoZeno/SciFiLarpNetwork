# SPEC-26: Hacking rules and game settings

## Purpose and prerequisites

Define the rules that decide a simulated hacking challenge (required hits, duration, penalty characters, keystroke cap, and the hacking level a challenge uses), and the administrator-editable game settings that parameterize them. The settings are edited in the Avalonia management application and enforced by the backend.

Implement this after [SPEC-24](SPEC-24-event-store-runtime.md), [SPEC-25](SPEC-25-administrator-authentication-authorization.md), the management application foundation ([SPEC-06](SPEC-06-avalonia-management-app.md)), and [MANAGER-02](legacy-features/manager-tabs/MANAGER-02-skills.md), which supplies the hacking skill flags. It precedes [CROSSCUT-04](CROSSCUT-04-hacking-simulation-rules-update.md), [SPEC-27](SPEC-27-hacking-plugs.md), and [CROSSCUT-03](CROSSCUT-03-server-authoritative-hacking-challenges.md), which consume these rules. This specification does not create or resolve challenges.

The rationale, the legacy comparison, and the success-probability tables behind the default values are in [hacking-system-comparison.md](../references/hacking-system-comparison.md) and [old-hacking-system.md](../references/old-hacking-system.md).

This specification supersedes the required-hits formula (`minimumHits + baseHits(difficulty) − hackingLevel`), the fixed four penalty characters, the fixed 60-second duration, and the penalty/target rotation rules in CROSSCUT-02 and CROSSCUT-03. Those documents point here.

## Design intent

- The player's keystrokes decide the result. The player cannot know the target key, so each keystroke is a random event that the player produces. Typing speed matters, in the way a LARP fighter's own skill matters next to the character's combat skill. A character's hacking level lets the player attempt harder hacks, and the player's typing still counts.
- A cap on keystrokes per second prevents key smashing, in the same way LARP rules forbid weapon tapping.
- The reference rules system (the original Star Wars LARP rules) limits hacking level and difficulty to 4. That is the practical reference for the defaults. The code must not impose any maximum on hacking level, effective level, or difficulty.
- Rules systems that want levels above 4, or a harder hacking game with higher rewards, tune the settings in this specification, mainly the penalty characters, the keystroke cap, the duration, and the hit counts.

## Rules

### Levels and access

- A character's **hacking level** is derived from skills as defined by MANAGER-02: the “Improves Hacking” skills count only when the character also owns a skill with “Access Hacking”.
- A challenge uses the **effective hacking level**: the hacking level plus any boost from a plug (SPEC-27). Effective level may be fractional (at most two decimal places) and has no maximum.
- A character whose hacking level is below 1 cannot start a challenge, with or without a plug. The refusal is a stable, localized error.
- **Difficulty** is a whole number of at least 1 and has no maximum. The consuming feature owns each target's difficulty.

### Required hits

`requiredHits = max(1, requiredHitsBase + requiredHitsPerDifficulty × difficulty)`

The hacking level does not change the number of hits. Difficulty does.

### Duration

`durationSeconds = min(3600, round(durationBaseSeconds + durationSecondsPerLevel × effectiveLevel))`

The hacking level changes the time available. The 3600-second ceiling is a fixed safeguard against unbounded levels, not a setting.

### Penalty characters

- The setting `penaltyCharacterCount` is the number of penalty characters, `N`. The default is 0, which means no penalties.
- The N penalty characters are distinct from each other and from the target, and are drawn from the challenge alphabet. N is at most the alphabet size minus one.
- Pressing a penalty character reduces progress by one, clamped at zero. When progress is zero, a penalty has no effect.
- Penalties raise the difficulty of hacking for every level. Rules systems use them to make hacking harder in return for higher rewards.

### Target and penalty rotation

- After every accepted attempt, whether it was a hit, a penalty, or an ordinary key, the server draws a new target character and new penalty characters from a cryptographically secure random source.
- With this rule each key has a fixed chance of being the target, and no key-elimination strategy is better than another. Without it, a player could cycle through the keys without repeating any and find the target in at most 36 attempts.

### Keystroke cap

- The setting `maxKeystrokesPerSecond` is the maximum number of attempts the server accepts per second for one challenge. The default is 3.
- The sustained accepted rate never exceeds the cap. The limiter may allow a burst of at most one extra attempt to absorb network jitter, and the chosen design is documented.
- A throttled attempt is ignored. It does not hit, does not penalize, does not use time, and does not trigger cooldowns or other consequences. It returns the stable rate-limit result from CROSSCUT-03.
- The cap is a game rule and is separate from the request-flood protection that bounds abusive traffic. Flood protection stays above the cap and does not depend on it.

## Game settings

| Setting | Default | Validation |
|---|---|---|
| Penalty characters (`penaltyCharacterCount`) | 0 | Whole number from 0 to the alphabet size minus one. |
| Keystroke cap (`maxKeystrokesPerSecond`) | 3 | Whole number from 1 to 20. |
| Hits base (`requiredHitsBase`) | 4 | Whole number from 0 to 50. |
| Hits per difficulty level (`requiredHitsPerDifficulty`) | 1 | Whole number from 0 to 20. The required hits for difficulty 1 must be at least 1. |
| Duration base seconds (`durationBaseSeconds`) | 44 | Whole number from 0 to 600. |
| Duration seconds per level (`durationSecondsPerLevel`) | 12 | Whole number from 0 to 120. The duration for effective level 1 must be at least 10 seconds. |

The numeric bounds are validation limits chosen to prevent nonsensical values. They can be revised without changing the rules.

Default results (the test vectors for both implementations):

| Difficulty | Required hits |
|---|---|
| 1 | 5 |
| 2 | 6 |
| 3 | 7 |
| 4 | 8 |
| 8 | 12 |
| 10 | 14 |

| Effective level | Duration |
|---|---|
| 1 | 56 s |
| 1.5 | 62 s |
| 2 | 68 s |
| 3 | 80 s |
| 4 | 92 s |
| 8 | 140 s |

The challenge alphabet is the one configured under CROSSCUT-03 (32 characters by default). The player can press 36 keys, so a random key is the target with a chance of 1 in 36.

## Settings lifecycle

- The settings are server-side game data. They are not the client-local `ManagementSettings` of the management application.
- Store them in one event-sourced settings stream with a fixed, deterministic stream identifier, following [SPEC-24](SPEC-24-event-store-runtime.md). When no event exists, the defaults above are in force. Do not require a bootstrap event.
- Every edit is an authorized, validated, auditable command that loads the stream with `FetchForWriting`, carries the expected version, and appends one event containing the complete new values. A stale edit is rejected with a conflict, never overwritten. “Restore defaults” is an edit that appends an event with the default values, so history is kept.
- Every challenge copies the settings in force, and the settings stream version, into its initiation event. Editing the settings never changes a challenge that has already started.
- Only administrators with write permission can change settings. Administrators with read permission can view them. The player client never reads or writes the settings. It receives only what a challenge returns (CROSSCUT-03).

## Management application

- Add a “Hacking settings” entry to the management navigation, visible to administrators who can read settings and editable only by administrators who can write.
- Show every setting with its label, default, and validation message. Provide “Restore defaults”, “Save”, and “Discard changes”. Confirm before saving changes and surface conflict, validation, and persistence errors clearly.
- Show a preview table of required hits per difficulty and duration per effective level (1 to 8 by default) for the values being edited, including unsaved values. The backend computes the preview, so the client holds no copy of the rules.
- All text is localized in English and Italian ([SPEC-20](SPEC-20-language-and-localization.md)).

## API

- Expose the settings and the preview as typed MVC/Siren resources and actions with stable machine-readable errors, as defined by [SPEC-05](SPEC-05-aspnetcore-mvc-backend.md). Siren links are discoverability only. Every request is authenticated and authorized independently.
- Rule calculations (required hits, duration, effective level, penalty count validation, keystroke cap) are pure, deterministic domain functions used by the settings preview and by challenge creation. The frontend demo implements the same formulas (CROSSCUT-04), and both implementations are verified against the same published test vectors.

## Acceptance criteria

- The defaults are in force on a new installation with no settings event.
- Administrators can view and edit all six settings in the management application. All changes are validated, authorized, auditable, and concurrency-safe.
- The calculations match the formulas and test vectors above, with no maximum on difficulty, level, or effective level (the duration ceiling excepted).
- A hacking level below 1 is refused before a challenge starts, with or without a plug.
- Changing a setting never changes a challenge already in progress.
- The keystroke cap and penalty count are enforced by the server. Client-side behavior is a usability aid only.
- The management client, the backend API, and the English/Italian resources handle invalid input, stale edits, missing permissions, and persistence failures clearly.

## Verification

- xUnit tests cover each formula with the vectors above, boundary values, fractional levels, very high levels and difficulties, the duration ceiling, the level-0 refusal, and every validation rule.
- Tests cover authorization (read-only versus write), event emission, concurrency conflicts, restore-defaults history, and the immutability of a started challenge's copied settings.
- A test-only calculator of the exact success chance (a Markov chain over hits per keystroke, using a 1/36 hit chance and an N/36 penalty chance) verifies that the default settings keep the documented success table within one percentage point. It guards the tuning and is not a product feature.
- API tests verify discovery links and actions for readers and administrators, and that clients cannot supply settings to a challenge.
- Management-client tests cover viewing, editing, validation messages, restore defaults, the preview table, conflict handling, and read-only mode.

## Open decisions

- **Target and penalty rotation on every keystroke** is proposed and not yet explicitly approved. The previous rules rotated the target only after a hit.
- **The default formulas and values** (4 + difficulty hits, 44 + 12 × level seconds) were tuned analytically for a player typing at the cap. They need a playtest measuring real typing speed. Players who type more slowly than the cap have much lower chances (see the speed tables in the comparison document).
- **An estimated chance of success** in the settings preview is not included. It can be added later if administrators need it for tuning.
- **The bounds** in the settings table are proposed values.
