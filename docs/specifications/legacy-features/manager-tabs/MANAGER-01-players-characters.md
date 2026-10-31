# Manager tab: Players and characters

**Legacy entry:** `HolonetManager` → “Giocatori & Personaggi” (`HolonetWinControls\Personaggi\ControlPersonaggi`).

## Observed behavior

Lists players in pages of 50 and, when one player is selected, lists that player's characters. Supports name or numeric lookup for players and characters, player creation/editing, character creation/editing, GUID checking, and PDF printing of a character card plus advanced-ability cards when applicable.

## Acceptance criteria

- An administrator can browse players and their associated characters with stable paging, and search either collection by name or identifier.
- Creating and editing a player or character validates required fields, uniqueness, relationships, and domain constraints and gives clear success/error feedback.
- Character details include the generic identity, network/faction, status, skills, credentials, protection, and other fields approved by the domain model; sensitive credential material is never displayed in printouts or logs.
- Administrators can issue/regenerate QR identity credentials and detect invalid or duplicate identifiers without silently changing unrelated records.
- Character cards can be previewed and exported as printable PDF; advanced-skill information is included only when present and authorized.
- Search results, paging, empty states, and selected-player context remain consistent after add/edit operations.
