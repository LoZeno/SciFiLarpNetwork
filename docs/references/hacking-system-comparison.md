# Hacking System: Old vs New Comparison and Recommended Design

> This document compares the legacy Holonet3 hacking roll with the new typing prototype, lists the chance of success for every hacker level (1-4) against every difficulty (1-4), and records the design that was chosen. Old-system facts come from the Holonet3 code and are listed in `old-hacking-system.md`. Probabilities for the new system come from a model of the prototype code (marked **inferred**), not from a measured play session.
>
> The chosen design is specified in [SPEC-26](../specifications/SPEC-26-hacking-game-settings.md) (rules and settings), [CROSSCUT-04](../specifications/CROSSCUT-04-hacking-simulation-rules-update.md) (interaction), and [SPEC-27](../specifications/SPEC-27-hacking-plugs.md) (plugs).

## Goals

- A character with hacking level 0 cannot start a hack.
- Hacking level 1 against difficulty 4 has a very small chance.
- Hacking level 4 against difficulty 1 is very likely to win.
- The chance grows with hacker level and falls with difficulty. It need not be exactly linear.
- The typing decides the result. The player cannot know the target key, so each keystroke is a random event that the player produces. Typing speed matters, in the same way that a fighter's own skill matters in a LARP next to the character's combat skill.
- A cap on keystrokes per second prevents key smashing, in the same way LARP rules forbid weapon tapping.
- Hacking level and difficulty have no maximum in code. The reference rules system limits both to 4, so 4 is the practical reference for the defaults.
- Penalty characters, the keystroke cap, and the minigame duration are configurable game settings, edited in the Management application.
- Two kinds of plug: a *guarantee plug* (always succeeds up to a difficulty level) and a *boost plug* (adds effective hacker level, fractions allowed, up to a difficulty level). Each can be single-use, multi-use or permanent.

## Old system (Holonet3)

`chances = 10 + 20 × hackerLevel − 10 × difficulty`, minimum 1. Roll 0-98, success when `roll <= chances`.

| Hacker level | Difficulty 1 | Difficulty 2 | Difficulty 3 | Difficulty 4 |
|---|---|---|---|---|
| 1 | 21% | 11% | 2% | 2% |
| 2 | 41% | 31% | 21% | 11% |
| 3 | 62% | 52% | 41% | 31% |
| 4 | 82% | 72% | 62% | 52% |

Level 1 against difficulty 4 is very unlikely, which matches the goal. Level 4 against difficulty 1 is only 82%, which does not.

## New prototype: what the code does

Sources: `Frontend/src/hacking/TheatricalHackingSimulation.tsx`, `TheatricalHackingTestPage.tsx`, `challengeRules.ts`.

- Required hits: `max(1, minimumHits + difficulty − max(0, hackerLevel))`. The test page uses `minimumHits = 1`.
- The target character is picked from 32 characters (`ABCDEFGHJKLMNPQRSTUVWXYZ23456789`). The player can press 36 keys (A-Z, 0-9; the physical keyboard also accepts `'`). Keys outside the 32 are always neutral.
- 4 penalty characters each remove one hit, but only when the player already has at least one hit.
- The deadline is 60 seconds.
- The target stays the same after a miss. It changes only after a hit. Penalties change only when the hit count changes.
- Penalty keys and the hint button are shown only when `showTestOnlyFeatures` is on.
- The component rejects any server reply whose penalty list does not have exactly 4 distinct characters.

### Model (inferred)

Each keystroke is a random key out of 36. The chance of a hit is 1/36, and the chance of a penalty is N/36 when the player has at least one hit (N penalty characters). The chance of success within K keystrokes is computed exactly with a Markov chain. The player's typing speed and the duration set K. For example, 60 seconds at 2.5 keys per second is K = 150.

### Chance of success of the current prototype

Rows are required hits. Columns are the number of keystrokes in 60 seconds. There are 4 penalties.

| Required hits | 60 | 120 | 240 | 480 |
|---|---|---|---|---|
| 1 | 82% | 97% | 100% | 100% |
| 2 | 23% | 42% | 67% | 90% |
| 3 | 5% | 10% | 21% | 39% |
| 4 | 1% | 2% | 5% | 11% |

Grid with 120 keystrokes (2 per second):

| Hacker level | Difficulty 1 | Difficulty 2 | Difficulty 3 | Difficulty 4 |
|---|---|---|---|---|
| 1 | 97% | 42% | 10% | 2% |
| 2 | 97% | 97% | 42% | 10% |
| 3 | 97% | 97% | 97% | 42% |
| 4 | 97% | 97% | 97% | 97% |

### Findings

1. **Only the gap `difficulty − level` matters.** Level 1 against difficulty 1, level 2 against difficulty 2 and level 4 against difficulty 4 are identical. Every case where the level is at least the difficulty is a near-sure win.
2. **The grid is not spread across the range.** Half of the cells are one value.
3. **The target key can be found by elimination.** The target does not change after a miss, so a player who never repeats a key finds it in 36 keystrokes at most. With four penalties this helps little (4 hits needed: 9% against 5% at 240 keystrokes). With no penalties it decides the game: in a simulation of the first draft of the new design (no penalties, 57 + 13 × level seconds), a never-repeat player won level 1 against difficulty 4 about 82% of the time, against 12% for random typing. The chosen design therefore changes the target after every keystroke.
4. **Typing speed changes the result a lot.** This is accepted as part of the design, and the keystroke cap limits the fast end.

### Errors in the first version of this document

| Earlier claim | What the code and model show |
|---|---|
| 30 characters | 32 target characters and 36 keys. |
| About 20 keystrokes per minute | A normal typist presses far more. 20 keystrokes is an unrealistic low case. |
| 1 hit about 50%, 2 about 30%, 3 about 17% at 20 keystrokes | The model gives 43%, 6% and 0.8%. |
| Hacker levels above 4 in the comparison tables | Only Hacking I-IV exist in the old rules. |
| Change penalties from 4 to 1 in the backend | The frontend also requires exactly 4 penalties and would reject the reply. |
| Required hits `1 + floor((D − L) / 2)` | At 120 keystrokes, almost every cell is 97%, and level 1 against difficulty 4 is 67%. This fails the goals. |
| Remove penalties, shrink the pool, extend the time, add a hint | None of them gives control over the grid. Removing penalties without changing the target rule enables the elimination strategy in finding 3. |

## Chosen design

The result stays decided by the player's keystrokes. Two separate settings, one tied to each side, keep the grid monotone at every typing speed.

- **Difficulty sets the hits needed.** `requiredHits = requiredHitsBase + requiredHitsPerDifficulty × difficulty`, default `4 + difficulty` (5, 6, 7, 8 for difficulty 1-4).
- **Hacker level sets the time.** `durationSeconds = durationBaseSeconds + durationSecondsPerLevel × effectiveLevel`, default `44 + 12 × level` (56, 68, 80, 92 seconds for levels 1-4). Effective level is the hacker level plus any plug boost, and may be fractional.
- **Penalty characters are a setting.** The default is 0. A rules system can set N penalty characters to make hacking harder for higher rewards.
- **The keystroke cap is a setting.** The default is 3 keystrokes per second.
- **The target and penalty characters change after every keystroke,** so each key has exactly a 1/36 chance and no strategy beats random typing.
- **Duration, hits, penalties, and the cap are edited in the Management application.** They are server-side game settings.
- **No maximum.** The code accepts any hacker level, effective level, or difficulty.
- **Level 0 cannot open the minigame,** and plugs do not change that.

The defaults were chosen by fitting the chance to the target `50 + 15 × (level − difficulty)` percent (limited to 5-95%) for a player typing at the cap. The fit is approximate.

### Chance of success with default settings

Rows are hacker level, columns are difficulty. Typing speed is in keystrokes per second.

**3 keys per second (the default cap; nobody can type faster)**

| Level | D1 | D2 | D3 | D4 |
|---|---|---|---|---|
| 1 | 50% | 32% | 19% | 10% |
| 2 | 67% | 50% | 34% | 21% |
| 3 | 80% | 66% | 50% | 35% |
| 4 | 88% | 78% | 65% | 50% |

**2.5 keys per second**

| Level | D1 | D2 | D3 | D4 |
|---|---|---|---|---|
| 1 | 35% | 20% | 10% | 4% |
| 2 | 51% | 33% | 20% | 10% |
| 3 | 65% | 48% | 32% | 19% |
| 4 | 77% | 62% | 46% | 31% |

**2 keys per second**

| Level | D1 | D2 | D3 | D4 |
|---|---|---|---|---|
| 1 | 20% | 9% | 4% | 1% |
| 2 | 33% | 18% | 9% | 4% |
| 3 | 46% | 29% | 16% | 8% |
| 4 | 58% | 40% | 25% | 14% |

**1.5 keys per second**

| Level | D1 | D2 | D3 | D4 |
|---|---|---|---|---|
| 1 | 8% | 3% | 1% | 0% |
| 2 | 15% | 7% | 2% | 1% |
| 3 | 24% | 12% | 5% | 2% |
| 4 | 34% | 19% | 9% | 4% |

What this achieves and what it does not:
- The chance rises with level and falls with difficulty in every cell at every speed. Both level and difficulty matter, so level 1 against difficulty 1 is not the same as level 4 against difficulty 4.
- At the cap, level 1 against difficulty 4 is 10% and level 4 against difficulty 1 is 88%. That is close to the goals, not exactly "very little" and "almost guaranteed".
- **Typing speed is the largest variable.** The tables are tuned for a player typing at the cap. A player at 2 keys per second has 58% at level 4 against difficulty 1, and 1% at level 1 against difficulty 4. The goal of near-certain wins at the top end is met only for players who type at the cap. Measure real typing speeds with the test page before fixing the defaults.
- A steeper grid is possible by raising `requiredHitsPerDifficulty` or lowering `durationSecondsPerLevel`, at the price of lower chances for slow typists.

### Higher levels (no maximum)

With no penalties, the same formulas scale to levels above 4. Typing at the cap, with `4 + difficulty` hits and `44 + 12 × level` seconds:

| Level | D1 | D2 | D4 | D6 | D8 |
|---|---|---|---|---|---|
| 1 | 50% | 32% | 10% | 2% | 0% |
| 2 | 67% | 50% | 21% | 6% | 1% |
| 4 | 88% | 78% | 50% | 24% | 9% |
| 6 | 97% | 92% | 75% | 50% | 26% |
| 8 | 99% | 98% | 90% | 73% | 50% |

Level equal to difficulty is 50% from level 4 upward, so the scaling stays balanced without any extra rule.

### Penalty characters

Penalty characters make every hack harder. They do not make higher levels scale better, because a penalty removes progress at a fixed rate that more time does not offset. Typing at the cap, default hits and time:

| N penalty characters | L1 D1 | L1 D4 | L4 D1 | L4 D4 |
|---|---|---|---|---|
| 0 (default) | 50% | 10% | 88% | 50% |
| 1 | 16% | 1% | 33% | 7% |
| 2 | 5% | 0% | 10% | 1% |

A rules system that enables penalties should also raise the duration (`durationBaseSeconds`, `durationSecondsPerLevel`) or lower the hits so that the chances come back to a playable range. The Management application shows the resulting hits and duration for each difficulty and level.

### Plugs

| Plug | Rule |
|---|---|
| Guarantee plug, level N | The hack succeeds when the difficulty is N or lower. The typing game is not played. A use is spent when the plug is applied. |
| Boost plug, +b, ceiling C | When the difficulty is C or lower, effective level = level + b. Fractions are allowed. A use is spent on every attempt, whether the hack then succeeds, fails, or expires. |
| Single-use, limited, permanent | Both kinds use any of the three modes. A permanent plug is never exhausted. |

A boost of +0.5 level adds 6 seconds with default settings. Example: difficulty 3 (7 hits), typing at the cap:

| Effective level | Seconds | Chance |
|---|---|---|
| 1 | 56 | 19% |
| 1.5 | 62 | 26% |
| 2 | 68 | 34% |
| 2.5 | 74 | 42% |
| 3 | 80 | 50% |

A plug that does not apply (difficulty above N or above C) is rejected and spends no use. Level 0 cannot hack even with a plug. The old rule needed the plug level to be strictly greater than the difficulty. The new rule uses "less than or equal". See SPEC-27 for the full rules.

## Decisions

| Decision | Status |
|---|---|
| Typing decides the result (no server dice) | Decided |
| Typing speed is an accepted factor | Decided |
| Penalty characters are a setting, default 0, edited in the Management application | Decided |
| Keystroke cap is a setting, default 3 per second, edited in the Management application | Decided |
| Minigame duration is a setting, edited in the Management application | Decided |
| A boost plug spends one use on every attempt | Decided |
| A guarantee plug spends a use when it is applied | Decided |
| A guarantee plug works for difficulty up to and including its level (`<=`) | Decided |
| No maximum for hacker level, effective level, or difficulty in code | Decided |
| About 15 points per level, not necessarily linear | Decided as guidance; the defaults above are a first fit |
| Target and penalties change after every keystroke | Proposed, needs approval |
| Hits depend on difficulty, time depends on level (`4 + D`, `44 + 12 × L`) | Proposed, needs a playtest of real typing speed |
| Plug codes: store only a hash, show the code once | Proposed, needs approval |
| How a character obtains a plug (bearer code, as in the old system) | Open |

## Implementation

The work is split into three specifications, in this order (see `SPEC-22-implementation-order.md`, rows 69-71):

1. [SPEC-26](../specifications/SPEC-26-hacking-game-settings.md): rules, game settings, and the Management application editor.
2. [CROSSCUT-04](../specifications/CROSSCUT-04-hacking-simulation-rules-update.md): the theatrical component and demo page, with any number of penalties and the keystroke cap.
3. [SPEC-27](../specifications/SPEC-27-hacking-plugs.md): guarantee and boost plugs and their editor.

[CROSSCUT-03](../specifications/CROSSCUT-03-server-authoritative-hacking-challenges.md) (row 73) then applies them on the server.

## How the numbers were produced

Exact Markov chain over hits per keystroke (hit with chance 1/36, penalty with chance N/36 when hits are above 0). The never-repeat strategy was simulated with 40,000 runs per cell. The default hits and duration were found by searching hits `a + b × difficulty` and seconds `c + d × level` for the best fit to the target at 3 keystrokes per second. The scripts are not in the repository. SPEC-26 requires a test-only exact-odds calculator that keeps the default table within one percentage point. Real typing speeds should be measured with the demo page before the grid is fixed.
