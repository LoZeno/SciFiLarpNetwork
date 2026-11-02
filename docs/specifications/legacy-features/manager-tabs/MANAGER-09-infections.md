# Manager tab: Character conditions

**Legacy entry:** `HolonetManager` → “Infezioni” (`HolonetWinControls\Infezioni\ControlInfezioni`).

## Observed behavior

Lists characters in pages of 50, supports name search, and shows conditions/infections assigned to the selected character. Administrators can create a condition, assign one to a character, or remove an assignment after confirmation.

## Acceptance criteria

- Administrators can find a character and view the character's current assigned conditions and their descriptions.
- The catalog distinguishes reusable condition definitions from a character's assignment and records assignment/removal in the event history.
- Authorized administrators can create a condition and assign an existing condition to a selected character; invalid or duplicate assignments are rejected clearly.
- Removing an assignment requires confirmation, records who removed it and when, and preserves the condition definition and history.
- Character paging/search and condition detail refresh consistently after an assignment change.
