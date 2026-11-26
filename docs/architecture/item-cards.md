# Item cards

Item cards ([MANAGER-06](../specifications/legacy-features/manager-tabs/MANAGER-06-items-components.md)) are PDFs made by the backend with [Typst](https://typst.app), in the same sandbox as [character cards](character-cards.md). Each card is one copy of an item, with the copy's own QR code. Uploading an item-card template comes with [SPEC-32](../specifications/SPEC-32-item-card-templates.md); until then the default template embedded in the backend (`Items/ItemCards/item-cards.typ`) is used.

## How a PDF is made

1. The item collection advertises `print-item-cards` and `reprint-item-cards` to administrators, and `preview-item-cards` to readers and administrators (see the [Siren API profile](siren-api-profile.md)).
2. `IItemPrintService` collects the cards: a print creates the copies first, a reprint reads existing copies, and a preview uses the sample QR code `00000000000000000000000000000000`.
3. The renderer writes `cards.json` and runs Typst exactly as for character cards. The QR image is drawn by the backend (QRCoder) as SVG.

## The `cards.json` data

| Member | Type | Meaning |
|---|---|---|
| `language` | text | Two-letter language of the labels. |
| `labels` | object | Localized labels: `documentTitle`, `description`, `effect`, `charges`, `expires`, `archived`. |
| `cards` | list | One entry per card, in print order. |

Each card:

| Member | Type | Meaning |
|---|---|---|
| `itemType` | text | The item type's current name. |
| `name` | text | The item's name. |
| `qrCode` | text | The copy's QR code: 32 lower-case hexadecimal digits. |
| `qrSvg` | text | The QR code drawn as SVG; use `image(bytes(card.qrSvg), format: "svg")`. |
| `description` | text or `none` | The description, when set. |
| `effect` | text or `none` | The effect, when set. |
| `charges` | number or `none` | The charges, when set. |
| `expiresOn` | text or `none` | The expiry date as `YYYY-MM-DD`, when set. |
| `isArchived` | true/false | Whether the item is archived (only reprints include archived items). |

User content is printed as entered; cost, rarity and the picture are not in the data.

## Layout of the default template

An A4 page holds four cards; each row is one card, 105 × 74.25 mm per side. The front shows the type, the name and the QR code; the back shows the description, effect, charges and expiry date when set. Text that does not fit is shrunk, then cut with “…”, as on character cards.
