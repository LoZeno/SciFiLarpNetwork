# Old Codebase: Hacking System Mechanics

> **Note:** This document describes how the **original WebForms/.NET Framework codebase** (Holonet3) implemented the hacking simulation. It is a reference for understanding the legacy behavior, not a specification for the new codebase.

## Success/Failure Formula

The core hacking success calculation is defined in `HackingEngine.TentativoHacking` (`Holonet3/Holonet3/Utilities/HackingEngine.cs`).

### Algorithm

1. **Calculate base chances:** `10 + (hacker_level × 20)`
2. **Subtract difficulty:** `chances -= (difficulty_level × 10)`
3. **Apply minimum floor:** If `chances < 1`, set `chances = 1`
4. **Roll:** Generate a random integer from `0` to `98` (inclusive)
5. **Determine outcome:** If the roll is `≤ chances`, the hack succeeds

### Pseudocode

```
chances = 10 + (hackerLevel * 20) - (difficultyLevel * 10)
if chances < 1:
    chances = 1
roll = random(0, 98)
success = roll <= chances
```

### Examples

| Hacker Level | Difficulty | Calculation | Chance |
|---|---|---|---|
| 1 | 1 | `10 + 20 - 10 = 20` | ~20% |
| 3 | 2 | `10 + 60 - 20 = 50` | ~50% |
| 1 | 3 | `10 + 20 - 30 = 0` → floored to 1 | ~2% |
| 4 | 1 | `10 + 80 - 10 = 80` | ~82% |

`Random.Next(0, 99)` returns 0 to 98, and the hack succeeds when `roll <= chances`. The exact chance is therefore `(min(chances, 98) + 1) / 99`. The floor of 1 gives 2/99, which is about 2%, not 1%.

### Full grid (hacker level 1-4 against difficulty 1-4)

| Hacker level | Difficulty 1 | Difficulty 2 | Difficulty 3 | Difficulty 4 |
|---|---|---|---|---|
| 1 | 21% | 11% | 2% | 2% |
| 2 | 41% | 31% | 21% | 11% |
| 3 | 62% | 52% | 41% | 31% |
| 4 | 82% | 72% | 62% | 52% |

The best case for a level 4 hacker against difficulty 1 is 82%, not a near-certain win. Hacker level has twice the weight of difficulty (20 points against 10).

Hacking levels above 4 cannot exist, because only Hacking I-IV count (see below).

## Hacker Level Calculation

The hacker level (`LivelloHacker`) is the count of hacking skills the character possesses. Only these skills contribute:

- **Hacking I**
- **Hacking II**
- **Hacking III**
- **Hacking IV**

Each skill adds `1` to the level. A character with Hacking I, II, and III has a hacker level of `3`.

**Mental Fortitude (Forza Mentale)** is **not** included in the hacker level. It is a separate stat calculated from its own skill tree (Forza Mentale I–IV) and is used for other game mechanics (e.g., resisting mind control or psychic attacks).

## Other Rules Around the Roll

Source: `Holonet3/Holonet3/Hacking/TentativoHacking.ascx.cs` and `HackingEngine.cs`.

- **Level 0 cannot hack.** In release builds a character with `LivelloHacker == 0` is refused with "you are not a hacker according to the database". The check is compiled out in DEBUG builds.
- **Wait screen.** After the checks pass, a timer runs four ticks (33%, 66%, 99%, 100%) before the result is shown. Each hacker level subtracts 2 seconds from the timer interval. The wait is cosmetic; the result is a single roll at the last tick.
- **Retry lockout.** `AbilitatoHackingAccount` blocks a new attempt on the same account for 8 minutes after a failed attempt. A successful last attempt does not block. The mission, news and holodisk checks use `DataTentativo < Now.AddMinutes(8)`, which is true for any past attempt, so those targets have no effective lockout.
- **News hacking** additionally requires skill 177.
- **Some targets never allow a hack.** The `nulla` target type always refuses.

## Code Breaking Plugs (Spine di decodifica)

### How They Work

Some hackable entities accept a **Code Breaking Plug** (Spina). When presented:

1. The system looks up the plug code in the `CodiciHackings` database table.
2. If the plug exists and its `Livello` (level) is **strictly greater than** the target's `LivelloHacking` (difficulty level), the hack **succeeds automatically** — the probability formula is bypassed entirely. A plug of level 2 does not beat difficulty 2.
3. If no plug is entered, the plug is invalid, or the plug's level does not exceed the target's difficulty, the system falls back to the standard probability formula.

### Single-Use vs Multi-Use

In the old codebase, **all plugs are multi-use**. The plug code is only read from the database and compared against the target's difficulty. There is no deletion, no consumption flag, and no usage counter. The same plug code can be reused indefinitely.

### Availability

Not every hackable entity accepts plugs. The `AccettaSpina` flag on the hacking target determines whether the plug input is shown to the player.

## New Codebase Design Notes

In the new codebase, the following design decisions differ from the old system:

1. **Code Breaking Plugs must support single-use semantics.** Each plug type will either be single-use by default or have a flag to distinguish between single-use and permanent (multi-use) plugs. This allows game designers to control plug scarcity and balance.

2. **Not every hackable entity can be hacked using Code Breaking Plugs.** The ability to use a plug against a target will be determined per-entity type, not by a simple boolean flag. Some targets may be immune to plug-based bypasses.

3. **Two kinds of plug.** A *guarantee plug* always succeeds against difficulties up to and including a level, and spends a use when it is applied. A *boost plug* adds an amount of effective hacker level (fractions allowed) against difficulties up to a level, and spends a use on every attempt, whether or not the hack succeeds. Each kind can be single-use, limited-use or permanent. See `hacking-system-comparison.md` and SPEC-27.

4. **A character with hacking level 0 cannot start a hack,** with or without a plug, as in the old system.
