# Manager tab: Players and characters

**Legacy entry:** `HolonetManager` → “Giocatori & Personaggi” (`HolonetWinControls\Personaggi\ControlPersonaggi`, `InsertGiocatore`, `InsertPersonaggio`).


## Purpose

Let authorized administrators manage players, the characters they control, and administrator-defined character stats. Characters reference the network catalog of [MANAGER-11](MANAGER-11-networks.md). Talents and skills on a character are defined by [MANAGER-12](MANAGER-12-character-skills.md); character cards by [MANAGER-13](MANAGER-13-character-cards.md).


## Observed legacy behavior

- The tab listed players in pages of 50 and, when one player was selected, listed that player's characters. It supported name or numeric lookup for players and characters, player and character create/edit dialogs, a character GUID check, and PDF printing of character cards.
- A player had a numeric identifier, surname, first name, birth date, address, city, province, postcode, phone, join date, sex, a player type (`PG`, `PNG`, `Master`, `Senior`), an email, and a password.
- A character had a numeric identifier, its player, a name, a species from a catalog, a fixed type (biological or droid), experience points, sex, a franchise-specific “dark side” value, a creation date, a title, an alive flag and death date, a plain-text network password, hacking and encryption levels, an “enzyme”, a network, and a QR GUID.


## Product model

These rules replace the legacy personal-data fields, the player password, the character network password, the fixed character type, the species catalog, the “dark side” value, the hacking and encryption levels, and the enzyme. Hacking and cybersecurity levels are derived from skills (MANAGER-02); conditions are defined by MANAGER-09; player sign-in and character sign-in credentials are left to the sign-in specifications.

### Players

- A player has a generated identifier, a running number, a display name, an optional email, an optional phone, optional notes, a player type, and a status.
- Display names are required, are 1–64 characters after trimming, and are not unique.
- Email is optional, up to 254 characters, and must be a valid address when present. Phone is optional, up to 32 characters. Notes are optional, up to 2,000 characters.
- The player type is one of a fixed, localized list:

  | Type | English | Italian |
  |---|---|---|
  | Player | Player | Giocatore |
  | Non-player | Non-player | Non-giocante |
  | Game master | Game master | Game master |
  | Senior | Senior | Senior |

- A player's status is **Active** or **Inactive**. Marking a player Inactive makes every one of their Active characters Retired in the same command. Dead characters stay Dead.
- A player is not linked to an application Account in this specification. That link is left to the sign-in specifications ([WEBFORM-03](../webforms/WEBFORM-03-membership-login.md), [WEBFORM-04](../webforms/WEBFORM-04-registration.md)).

### Characters

- A character has a generated identifier, a running number, its player, a name, an optional title, an optional species, an optional gender, an optional type, experience points, a status, an optional non-default network, a QR identifier, and stat values.
- A player can have any number of characters. A character belongs to exactly one player, which does not change.
- Names are required, are 1–64 characters after trimming, and are not unique. Title, species, gender, and type are optional free text, 1–64 characters after trimming when present. They are user content and are not translated.
- Experience points are a whole number from 0 to 2,147,483,647.
- A character's status is **Active**, **Retired**, or **Dead**. A Dead character has a required death date. Status can change between any of the three, except that a character cannot become Active while its player is Inactive. Characters cannot be created for an Inactive player.

### Networks of a character

- Characters do not own network access. Whether a character can reach a network depends on the computer in front of them being registered for that network ([WEBFORM-39](../webforms/WEBFORM-39-network-selection.md)). This specification records only which networks a character may use.
- Every character may use the default network, when one exists. The character dialog shows the current default network as included and not removable. It is not stored on the character, so moving the default (MANAGER-11) applies to every character.
- A character may also have one non-default network. It is optional. When chosen, it must be an enabled network that is not the current default.
- If the chosen network later becomes the default, or is disabled, the character keeps it. The dialog shows the network once, marked as the default or as disabled.

### Running numbers

- Players and characters each get a running number, starting at 1, from separate sequences. Numbers are never reused, including after archiving.
- Running numbers are shown in lists and can be used to search.

### QR identifiers

- Every character gets a random, unique QR identifier when it is created. An administrator can regenerate it; the old identifier then no longer resolves to the character.
- An administrator can look up a QR identifier and see the character it belongs to, or a clear “not found” or “invalid” result.
- A QR identifier is public and printed. It is never an authentication credential. Any QR sign-in token is a separate, revocable secret governed by [WEBFORM-31](../webforms/WEBFORM-31-qr-login-prototype.md) and [CROSSCUT-01](../../CROSSCUT-01-qr-capture.md).

### Character stats

- Stats are administrator-defined values on characters (for example a “Dark Side” number from 0 to 5, or a “Teaching Competence” text such as “adept”). They have no effect on any application feature.
- A stat has a generated identifier, a name, and a type: **Number** or **Text**. Names are required, are 1–64 characters after trimming, and are unique among all stats (case- and Unicode-normalization–insensitive). The type cannot change after the stat is created.
- In the character dialog, an administrator can set values for existing stats and type a new stat name with its type. A new name creates the stat, which is then available on every character.
- Number values are whole numbers from 0 to 2,147,483,647. Text values are free text up to 64 characters after trimming. A character with no value for a stat shows 0 for a Number stat and an empty text for a Text stat.
- Stats can be renamed. Character values are stored by stat identifier, so a rename changes no character.
- Stats can be archived and unarchived. An archived stat keeps every character value, is hidden from the character dialog by default, and is clearly marked when shown.

### Archiving

- Nothing is deleted. Inactive players can be archived and unarchived. Dead or Retired characters can be archived and unarchived.
- Archived players and characters keep their history and relationships, are hidden from lists by default, and are clearly marked when shown.


## Acceptance criteria

- Readers and administrators can browse players in pages of 50 with a stable order, select a player to see their characters, and search players or characters by name or running number. Lists can show or hide archived entries. Only administrators can create, edit, change status, archive, unarchive, regenerate QR identifiers, or define stats.
- Validation enforces the rules above and gives clear, localized success and error feedback. Network references resolve to the MANAGER-11 catalog; edits cannot create dangling references.
- Edits preserve unrelated data: for example, editing a character's title does not change its stats, network, or QR identifier.
- Keep Player, Character, and application Account distinct. A character identifier or QR identifier is never treated as an application credential.
- Sensitive credential material, QR sign-in tokens, and other reusable secrets are never displayed in logs or ordinary API representations.
- Every change is an authorized, validated, auditable event-sourced command with optimistic concurrency. A stale edit is rejected with clear conflict feedback rather than overwriting a newer change.
- Search results, paging, empty states, and selected-player context remain consistent after add/edit operations.
- The backend API, the management client, and the English/Italian resources handle empty lists, archived entries, inactive players, a missing default network, disabled networks, invalid input, stale edits, and persistence failures clearly.


## Verification

- xUnit tests cover field rules, player types and statuses, the Inactive-player retirement rule, character statuses and death dates, network rules including the default and disabled cases, running numbers, QR identifier generation, regeneration and lookup, stat rules, archiving, authorization, event emission, and concurrency conflicts.
- API tests verify discovery links and actions for readers and administrators, validation and conflict errors, and that readers cannot change anything.
- Management-client tests cover listing, paging, searching and selection, the player and character dialogs, stat creation, status changes, QR regeneration and lookup, archive confirmation, and error display.
