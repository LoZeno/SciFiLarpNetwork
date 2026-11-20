# Manager tab: Character cards

**Legacy entry:** `HolonetManager` → “Giocatori & Personaggi” → print button (`HolonetWinControls\Personaggi\ControlPersonaggi`, `Reports\CartelliniPersonaggio`, `Reports\CartelliniAbilitaAvanzate`).


## Purpose

Let administrators preview and export a character's printable card, and an advanced-skill card when the character owns advanced skills. Characters are defined by [MANAGER-01](MANAGER-01-players-characters.md) and their skills by [MANAGER-12](MANAGER-12-character-skills.md).


## Observed legacy behavior

- The character card showed the player and character numbers, the player's full name, the character name and title, a QR code of the character GUID, sex, species, experience points and free points, hit points, “Toughness” and “Mental Strength” values counted from skills named by hand, the skills grouped by talent with their specifics notes, and the dates the card was valid for.
- The card also printed the character's network password in plain text.
- The advanced-skill card listed each owned advanced skill with its description.
- Both cards were written as a PDF file to a chosen folder.


## Product model

These rules replace the legacy fixed “hit points”, “Toughness”, and “Mental Strength” values, the printed network password, the separate advanced-skill card, and the hard-coded report layout.

### Card content

- **Front:** the player's running number and display name; the character's running number, title, and name; the character's QR identifier (MANAGER-01) as a QR image; and the character's gender, species, and type when present.
- **Back:** experience points and free points (MANAGER-12); the value of every card flag (MANAGER-02), including flags whose value is 0 (card flags cannot be archived; if archiving is added later, archived flags are left out); the value of every stat that is not archived (MANAGER-01), leaving out Text values that are empty; and the character's talents in picked order, each with the skills bought through it and their purchase count when it is above 1. A talent with no skills is still listed, as on the legacy card.
- Advanced skills are listed with the other skills and are marked (the default card prints them in italics). There is no separate advanced-skill card.
- Skill descriptions and specifics notes are not printed.
- An archived character can be printed; its back then says **Archived** in the requested language.
- Hit points and card validity dates are not printed. Validity dates are left to event printing (MANAGER-03, MANAGER-10).
- Sensitive credential material, QR sign-in tokens, and other reusable secrets are never on a card. The QR identifier is public (MANAGER-01).

### Layout

- An A4 page holds four cards in a 4 × 2 grid. Each row is one card: the front on the left and the back on the right, each 105 × 74.25 mm, with thin cut lines.
- Several characters can be printed together, up to 200 cards per request; they fill pages of four in the order requested.
- When text does not fit, the default card shrinks it; if it still does not fit, the text is cut and ends with “…”.
- Card labels (for example “XP” or “Archived”) are localized in the requested language. User content (names, titles, skill names, stat names and values, card flag names) is printed as entered.

### PDF generation

- The backend builds the PDF so that event printing (MANAGER-10) can reuse it. Readers and administrators can print cards, because printing changes nothing.
- The PDF is rendered by [Typst](https://typst.app) from a Typst template (`.typ`) and a JSON file holding the cards and the localized labels, as described in the [character cards guide](../../../architecture/character-cards.md). This specification ships one default character-card template, embedded in the backend. User-supplied templates are defined by [SPEC-29](../../SPEC-29-card-templates.md).
- The QR image is made in the backend as SVG by QRCoder and passed in the card data, so templates need no Typst packages.
- Rendering uses only the fonts built into Typst and never reaches the network.

### Management client

- In the roster, an administrator or reader selects one or more characters and chooses **Preview cards** or **Export cards**.
- **Preview cards** saves the PDF to a temporary file and opens it in the computer's default PDF viewer. **Export cards** asks where to save the PDF. The file is left for the viewer; each preview deletes preview files more than a day old.
- While several characters are selected, **Edit**, **Archive**/**Unarchive**, and **New QR identifier** are off, because they act on one character.
- Reloading the character list keeps only the current character selected.
- Known limit: the selection covers one page of the list, so one PDF holds at most 50 cards from the app, although the API accepts 200. Printing every matching character may come later (for example with MANAGER-10).
- Known limit: the backend loads each card's character, player, and skills one after another, so 200 cards take up to 600 queries. Loading them in batches is the fix if printing gets slow.


## Runtime dependencies

Verified as required by [SPEC-21](../../SPEC-21-offline-intranet-operation.md) and the [offline operation guide](../../../architecture/offline-operation.md):

| Dependency | Licence | Platforms | Offline use |
|---|---|---|---|
| Typst CLI `v0.15.1` | Apache-2.0 | Official release builds for `x86_64`/`aarch64` Windows (`pc-windows-msvc`), Linux (`unknown-linux-musl`, static), and macOS (`apple-darwin`), covering all six SPEC-07 RIDs | Runs locally with built-in fonts; no activation. Downloaded with a pinned SHA-256 at build time and bundled with the backend. |
| QRCoder | MIT | Managed .NET | No network use. |
| PdfPig (tests only) | Apache-2.0 | Managed .NET | Not shipped. |


## Acceptance criteria

- Readers and administrators can preview and export cards for one or more characters as a printable PDF with the content and layout above.
- Card flags and stats follow the archived and empty-value rules above; archived characters are marked as archived.
- Sensitive credential material, QR sign-in tokens, and other reusable secrets are never displayed on a card.
- Unknown character identifiers, an empty selection, and a rendering failure give clear, localized errors in the API and the management client.
- Card generation works offline on every supported platform.


## Verification

- xUnit tests cover the card data rules: content of the front and back, card flags with value 0, hidden archived flags and stats, hidden empty Text stats, skills grouped by talent with counts, the advanced mark, the archived mark, and localized labels.
- Integration tests render a real PDF with the bundled Typst and check its text, the page count for more than four characters, and the errors for unknown characters and an empty selection.
- API tests verify the print action for readers and administrators.
- Management-client tests cover selection, preview, export, and error display.
