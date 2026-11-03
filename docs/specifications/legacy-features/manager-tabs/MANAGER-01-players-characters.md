# Manager tab: Players and characters

**Legacy entry:** `HolonetManager` → “Giocatori & Personaggi” (`HolonetWinControls\Personaggi\ControlPersonaggi`).

## Observed behavior

Lists players in pages of 50 and, when one player is selected, lists that player's characters. Supports name or numeric lookup for players and characters, player creation/editing, character creation/editing, GUID checking, and PDF printing of a character card plus advanced-ability cards when applicable.

## Acceptance criteria

- An administrator can browse players and their associated characters with stable paging, and search either collection by name or identifier.
- Creating and editing a player or character validates required fields, uniqueness, relationships, and domain constraints and gives clear success/error feedback.
- Character details include the generic identity, configured network, status, configured skills, in-game protection, and other fields approved by the domain model. Network and skill references resolve to the catalogs established by MANAGER-11 and MANAGER-02; edits cannot create dangling references.
- Keep Player, Character, and application Account distinct. Create/edit operations enforce the approved player-character relationship and never treat a character identifier as an application credential.
- Administrators can issue/regenerate QR identity identifiers and detect invalid or duplicate identifiers without silently changing unrelated records. A printed/public character QR identifier is not an authentication credential; any QR sign-in token is a separate, revocable secret governed by WEBFORM-31 and CROSSCUT-01.
- Sensitive credential material, QR sign-in tokens, and other reusable secrets are never displayed in printouts, logs, or ordinary API representations.
- Character cards can be previewed and exported as printable PDF; configured skill information is included only when present and authorized.
- Search results, paging, empty states, and selected-player context remain consistent after add/edit operations.
