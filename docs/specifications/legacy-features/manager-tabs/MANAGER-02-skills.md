# Manager tab: Talents and skills

**Legacy entry:** `HolonetManager` → “Liste & Abilità” (`HolonetWinControls\Abilita\ControlAbilita`, `InsertLista`, `InsertAbilita`; data access in `CommonBusiness\Abilita\AbilitaManager`).

## Observed legacy behavior

- The tab listed skill groups (“attitudini”, called **talents** in this product), showed the skills of the selected group, and opened separate create/edit dialogs for groups and skills. Nothing could be deleted.
- A group had a hand-typed, upper-cased, immutable code (up to 10 characters), a required name (up to 50 characters), a description, and a type from a fixed Star Wars–specific list (biological, droid class, droid, implants, droid implants).
- A skill had a required name (up to 50 characters), a description, a cost in experience points (0–999), a multi-purchase flag, and an “advanced” flag used to print advanced-skill cards. A character's free points were its points minus the cost of each owned skill, multiplied by the number of purchases for multi-purchase skills.
- The database allowed a skill to belong to several groups, but the dialog only added a skill to the selected group. Prerequisite and effect tables existed but were never used.
- Game rules found skills by hard-coded numeric identifiers or names: the chemistry lab and substance identification required skill 1, the biochemistry lab and toxin identification skill 2, the technical workbench skill 13, 66, or 132, the hacking level was the number of “HACKING I–IV” skills owned, encryption level came from skills 10–12, and character cards counted skills named “ROBUSTEZZA” or “FORZA MENTALE” by name.

## Product model

These rules replace the legacy hand-typed codes, the fixed group type, and every hard-coded skill identifier or name.

### Talents

- A talent has a generated identifier, a name, and an optional description. There is no type and no hand-typed code.
- Talent names are required, are 1–64 characters after trimming, and are unique among all talents (case- and Unicode-normalization–insensitive).
- Talents can be archived and unarchived. Nothing is deleted.

### Skills

- A skill has a generated identifier, a name, an optional description, a cost (a whole number from 0 to 999), a multi-purchase flag, and an advanced flag.
- Skill names are required, are 1–64 characters after trimming, and are unique among all skills, whatever talents they belong to (case- and Unicode-normalization–insensitive).
- A skill is created first and then assigned to zero or more talents. A skill may belong to several talents. Assigning a skill to an archived talent is rejected; existing assignments to a talent that is later archived are kept.
- Skills can be archived and unarchived. Nothing is deleted.

### Game flags

A skill can carry any of these fixed game flags. They are product-defined, localized, and used by the player-facing features that the flag names:

| Area | Access flag | Improvement flag |
|---|---|---|
| Simulated hacking | Access Hacking | Improves Hacking |
| Cybersecurity | Access Cybersecurity | Improves Cybersecurity |
| Medical chemistry lab | Access Medical Chemistry Lab | Improves Medical Chemistry |
| Biochemistry lab | Access Biochemistry Lab | Improves Biochemistry |
| Technical workbench | Access Technical Workbench | Improves Item Creation |
| Substance identification | Gives Chemical Identification | — |
| Toxin identification | Gives Toxin Identification | — |

- An access or “gives” flag lets a character use the matching screen or ability.
- An improvement flag adds to the character's level in that area: each owned skill with the flag adds 1, and each additional purchase of a multi-purchase skill adds 1 more.
- An improvement flag counts only when the character also owns at least one skill with the matching access flag. For example, skills that improve hacking have no hacking effect on a character with no skill carrying “Access Hacking”.
- “Improves Hacking” supplies the attacker hacking level used by [CROSSCUT-03](../../CROSSCUT-03-server-authoritative-hacking-challenges.md); “Improves Cybersecurity” supplies the level used to raise the difficulty of challenges against a character's account. The consuming features define how each level is applied.
- This specification stores the flags on skills. The player-facing effects are implemented by the consuming feature specifications.

### Card flags

- Card flags are administrator-defined, named values that matter only on the printed character card (for example a setting's Toughness or Mental Strength). They grant no access and have no effect on any application feature.
- In the skill dialog, an administrator can select existing card flags and type one or more new flag names. A new name creates the card flag, which is then offered in the skill dialog for every skill.
- Card flag names are required, are 1–64 characters after trimming, and are unique (case- and Unicode-normalization–insensitive). They are user content and are not translated.
- A character's value for a card flag is the number of owned skills with that flag, counting each additional purchase of a multi-purchase skill. The character and printing specifications use these values.

### Prerequisites

- A skill's prerequisites are a list of requirement groups. A character meets the prerequisites when it meets every group, and it meets a group when it owns at least one skill in that group. A group with one skill is a mandatory skill; a group with several skills is a choice.
- Example: “Skill X requires Skill Y AND Skill Z AND (Skill W OR Skill Q)” is stored as the groups `[Y]`, `[Z]`, `[W, Q]`. Any and/or rule can be entered this way; for example “(A AND B) OR C” is entered as `[A, C]`, `[B, C]`.
- Validation rejects empty groups, a skill requiring itself, the same skill twice in one group, references to unknown skills, and any requirement loop (for example X requires Y and Y requires X, directly or through other skills, considering every skill in every group).
- A prerequisite may be an archived skill; the skill dialog shows a warning for it.
- Prerequisites are enforced when skills are assigned to characters, which is implemented by [MANAGER-01](MANAGER-01-players-characters.md).

## Acceptance criteria

- Readers and administrators can list talents and skills, select a talent, and see only the skills assigned to it. Lists can show or hide archived entries. Only administrators can create, edit, assign, archive, or unarchive.
- Administrators can create and edit talents, and create and edit skills with their name, description, cost, multi-purchase and advanced flags, talent assignments, game flags, card flags, and prerequisites.
- Validation enforces the rules above, including unique names, valid prerequisite groups with no loops, and no assignment to archived talents. Edits preserve unrelated relationships: for example, editing a skill's description does not change its talents, flags, or prerequisites.
- Archiving and unarchiving require confirmation in the management client. Archived talents and skills keep their history and relationships, are hidden from new selection by default, and are clearly marked when shown.
- Every change is an authorized, validated, auditable event-sourced command with optimistic concurrency. A stale edit is rejected with clear conflict feedback rather than overwriting a newer change.
- Updates are reflected in both lists without stale selection or accidental cross-talent edits.
- The backend API, the management client, and the English/Italian resources handle empty catalogs, archived entries, invalid input, stale edits, and persistence failures clearly.

## Verification

- xUnit tests cover name rules and uniqueness, prerequisite-group validation and loop detection, talent assignment, game and card flags, archiving/unarchiving, authorization, event emission, and concurrency conflicts.
- API tests verify discovery links and actions for readers and administrators, validation and conflict errors, and that readers cannot change anything.
- Management-client tests cover listing and selection, the create/edit dialogs, the archive confirmation, the archived-prerequisite warning, and error display.
