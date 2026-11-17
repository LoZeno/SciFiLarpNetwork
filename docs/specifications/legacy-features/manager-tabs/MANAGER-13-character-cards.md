# Manager tab: Character cards

**Legacy entry:** `HolonetManager` → “Giocatori & Personaggi” → print button (`HolonetWinControls\Personaggi\ControlPersonaggi`, `Reports\CartelliniPersonaggio`, `Reports\CartelliniAbilitaAvanzate`).


## Purpose

Let administrators preview and export a character's printable card, and an advanced-skill card when the character owns advanced skills. Characters are defined by [MANAGER-01](MANAGER-01-players-characters.md) and their skills by [MANAGER-12](MANAGER-12-character-skills.md).


## Observed legacy behavior

- The character card showed the player and character numbers, the player's full name, the character name and title, a QR code of the character GUID, sex, species, experience points and free points, hit points, “Toughness” and “Mental Strength” values counted from skills named by hand, the skills grouped by talent with their specifics notes, and the dates the card was valid for.
- The card also printed the character's network password in plain text.
- The advanced-skill card listed each owned advanced skill with its description.
- Both cards were written as a PDF file to a chosen folder.


## Acceptance criteria

- Character cards can be previewed and exported as printable PDF; configured skill information is included only when present and authorized.
- Sensitive credential material, QR sign-in tokens, and other reusable secrets are never displayed on a card.


## Open questions

These are to be answered before this specification is implemented.

1. Card content: the same as the legacy card, with MANAGER-02 card-flag values and MANAGER-01 character stats instead of the fixed values, and no password. Should card validity dates be left to events (MANAGER-03/MANAGER-10)?
2. Where is the PDF built: in the backend, so MANAGER-10 can reuse it, with the management client showing a preview and saving the file?
3. Which packages: PDFsharp/MigraDoc (MIT) or QuestPDF (free only under a revenue limit) for PDF; QRCoder (MIT) for QR images?
