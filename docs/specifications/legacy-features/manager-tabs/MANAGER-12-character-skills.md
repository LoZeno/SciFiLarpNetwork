# Manager tab: Character skills

**Legacy entry:** `HolonetManager` → “Giocatori & Personaggi” → character dialog (`HolonetWinControls\Personaggi\InsertPersonaggio`, `AggiungiAbilita`).


## Purpose

Let administrators choose a character's talents and buy skills for the character from those talents, within the character's experience points and the skill prerequisites defined by [MANAGER-02](MANAGER-02-skills.md). Characters themselves are defined by [MANAGER-01](MANAGER-01-players-characters.md).


## Observed legacy behavior

- A character chose up to five talents when created. The talent lists offered depended on a fixed character type (biological or droid); droids had extra “droid class” and “biological” talent slots.
- Skills could be bought only through one of the character's talents. Each purchase stored the skill, the talent it was bought through, a purchase count for multi-purchase skills, and an optional free-text “specifics” note (up to 50 characters).
- Free points were the character's experience points minus the cost of each owned skill, multiplied by the purchase count for multi-purchase skills. Nothing stopped free points going below zero.
- Prerequisite tables existed but were never enforced.


## Product model

### Talents of a character

- A character picks up to 5 talents. The limit is fixed at 5 in this specification.
- A character can only pick talents that are not archived. Talents picked before they were archived are kept.
- A talent cannot be removed from a character while the character owns a skill bought through it.

### Skill purchases

- A character owns each skill at most once: one purchase per skill, holding the talent it was bought through, a purchase count, and a specifics note. An administrator can change the talent of an owned skill to another owned talent the skill belongs to.
- A character can buy a skill only through one of its talents, and only when the skill belongs to that talent. Archived skills cannot be bought; skills bought before they were archived are kept.
- A skill without the multi-purchase flag is bought at most once. A multi-purchase skill has a purchase count from 1 to 999.
- Each purchase has an optional “specifics” note, 1–64 characters after trimming when present. It is user content and is not translated.
- A purchase is accepted only when the character meets the skill's prerequisites (MANAGER-02).

### Catalog changes after a purchase

Rules are checked when a skill is bought or its purchase count is raised: raising a count also needs the skill to belong to its talent, its prerequisites to be met, the skill not to be archived, and enough free points. Later catalog changes never remove or change a purchase:

- If a skill's prerequisites change, characters that already own it keep it.
- If a skill is taken off the talent it was bought through, or a talent or skill is archived, the purchase is kept with its talent.
- If the multi-purchase flag is turned off, the purchase keeps its count. The count can be lowered but not raised.
- If a skill's cost changes, free points are recalculated with the new cost (see below).

### Free points

- Free points are the character's experience points minus the current cost of each owned skill, multiplied by its purchase count.
- A purchase, an extra purchase, or a lower experience-point value that would make free points negative is rejected with a clear, localized error.
- Raising a skill's cost (MANAGER-02) is allowed even when an owner's free points become negative. The free points are then shown as negative with a warning, and the character cannot buy skills or raise purchase counts until free points are zero or more again.

### Removing a skill

- A skill cannot be removed (or its purchase count lowered to zero) while another owned skill needs it as a prerequisite, that is, while removing it leaves one of that skill's prerequisite groups with no owned skill. The error lists the dependent skills that must be removed first.

### Saving

- Talents and skills are saved together, as the full list for one character, against the character's version. Every rule is checked on the list being saved, so a skill and its prerequisite can be bought in the same save.
- Talents and skills can be changed on characters of any status, archived or not, like the other character details (MANAGER-01).
- A talent archived after being picked still counts toward the limit of 5.


## Acceptance criteria

- Administrators can pick and remove a character's talents, buy and remove skills, change purchase counts and specifics notes, and see free points update.
- Validation enforces the rules above, including the talent limit, buying only through owned talents, no archived talents or skills for new picks, prerequisites, non-negative free points, and the dependent-skill block on removal.
- Every change is an authorized, validated, auditable event-sourced command with optimistic concurrency. A stale edit is rejected with clear conflict feedback rather than overwriting a newer change.
- The backend API, the management client, and the English/Italian resources handle empty catalogs, archived entries, invalid input, stale edits, and persistence failures clearly.


## Future work

- A later story makes the number of talents configurable, and allows a different number of talents per character type. Character type is free text in MANAGER-01, so that story also needs a way to define types.


## Verification

- xUnit tests cover the talent limit, buying through owned talents, multi-purchase counts, specifics notes, prerequisites, free-point rules, the dependent-skill block, authorization, event emission, and concurrency conflicts.
- API tests verify discovery links and actions, validation and conflict errors, and that readers cannot change anything.
- Management-client tests cover picking talents, buying and removing skills, free-point display, and error display.
