# Manager tab: Items and components

**Legacy entry:** `HolonetManager` → “Oggetti & Componenti” (`HolonetWinControls\Oggetti\ControlOggetti`, `InsertOggetto`, `AggiungiComponente`, `CreaTipo`; data access in `CommonBusiness\Oggetti\OggettiManager`; cards in `Reports\CartelliniOggetto`).

## Observed behavior

Filters items by type, provides paged listing and text search, displays components for a selected item, and supports create, edit, clone, add/remove component, and printable item-card PDF generation. A single selected item can optionally be repeated for multiple physical copies.

In detail, the legacy tab:

- Stored for each item a name (up to 30 characters), a type, a required description, an effect, a required cost (a decimal number, 0 for priceless items), an optional number of charges, an optional expiry date (only a future date was kept), an availability chosen from a lookup list, and an image field that was never filled.
- Kept item types in a list that could only grow; a new type was refused when the same text already existed.
- Listed items 50 at a time ordered by name, filtered by type, and searched the name and the effect.
- Used other items as components: an ordered list where the same component could appear twice, with no check for an item containing itself.
- Cloned an item by copying its fields and components into a new item with the same or a new name.
- Created ten random QR codes per item. Each printed card used one of them at random, and printing added missing codes. Printing one item with “multiple” checked printed ten copies.
- Printed on the front the type, the name, a QR code and a logo, and on the back the description, the effect, the charges and the expiry date.

## Product model

Approved 2026-11-25. The player screens that read items are [WEBFORM-19](../webforms/WEBFORM-19-identify-item.md) (identify an item) and [WEBFORM-25](../webforms/WEBFORM-25-technical-workbench.md) (technical workbench). Event printing is [MANAGER-10](MANAGER-10-event-printing.md). Uploading an item-card template is [SPEC-32](../../SPEC-32-item-card-templates.md).

An item is a definition, like a class: it says what a kind of object is. The physical objects are its **copies**. Each printed card is one copy with its own QR code, and a copy can be deactivated when it is used up.

### Item types

- An item type has a generated identifier and a name. Names are required, 1–64 characters after trimming, without control characters, and unique among all item types (case- and Unicode-normalization–insensitive).
- Item types can be archived and unarchived. Nothing is deleted. Lists show them by name.

### Rarities

- A rarity (legacy “availability”) has a generated identifier and a name, with the same name rules as item types. Each LARP names its own rarities, for example Common, Uncommon and Rare.
- Rarities have an order set by administrators. A new rarity is added at the end; **Move up** and **Move down** swap it with the nearest rarity above or below that is not archived, so archived rarities, hidden by default, keep their places. Lists show rarities in this order.
- Rarities can be archived and unarchived. Nothing is deleted. An archived rarity keeps its place.

### Items

- An item has a generated identifier, a name, a type, a rarity, an optional description, an optional effect, an optional image, a cost, an optional number of charges, an optional expiry date, and its components.
- Names are required, 1–64 characters after trimming, without control characters, and unique among all items, archived ones included (case- and Unicode-normalization–insensitive).
- The type and the rarity are required. When an item is created, or its type or rarity changes, the new one must exist and not be archived. An edit may keep a type or rarity archived since.
- The description and the effect are each up to 4,000 characters after trimming, without U+0000. Line breaks are kept. Blank means not set.
- The cost is a whole number from 0 to 999,999 (0 for priceless items).
- Charges, when set, are a whole number from 1 to 999.
- The expiry is a date without a time. Any date is allowed, past ones included.
- Names, descriptions and effects are user content and are not translated.
- Items can be archived and unarchived. Nothing is deleted. An archived item cannot be printed, cannot be added as a component, and keeps its components and copies.

### Image

- An item may have one picture: PNG, JPEG or WebP, up to 2 MB, found from the file's first bytes as for network images ([MANAGER-11](MANAGER-11-networks.md)).
- The file is kept in the `ItemImages/` subfolder of the media folder (`Media:RootPath`), named by its identifier. Each upload gets a new identifier, so a file is never overwritten. Replaced and removed images are kept, as for network images ([SPEC-31](../../SPEC-31-media-folder-layout.md)). Events record only the identifier, type and size.
- The backend serves the image with its stored type and `X-Content-Type-Options: nosniff`.
- The image is not printed on the card.

### Components

- An item's components are other items, each with a quantity from 1 to 99. Their order does not matter. An item has at most 20 different components.
- A component must exist. Adding an archived component is refused; a component archived later is kept and marked.
- An item cannot contain itself, directly or through its components.
- Adding an item that is already a component is refused; change its quantity instead.
- Two items cannot have the same components with the same quantities, so the technical workbench never has two answers. Items without components are exempt.
- Removing a component asks for confirmation.

### Clone

- **Clone** opens the item dialog filled with the chosen item's fields, image and components, and a note that this is a clone. Saving creates a new item; the name must be changed, because names are unique, and the components must be changed, because recipes are unique.
- Copies are never cloned.

### Copies and QR codes

- An item gets no QR code when it is created. Each printed card is a new **copy** of the item with a new random QR code. The QR code is the copy's public identifier: 32 lower-case hexadecimal digits, as for characters ([MANAGER-01](MANAGER-01-players-characters.md)). It is never an authentication credential.
- A copy records its item, the print it belongs to, who printed it and when, and whether it is **active** or **deactivated**. New copies are active.
- Administrators can deactivate a copy and turn it back on, with a confirmation. The history keeps who did it and when. Later features deactivate copies as they are used, for example components consumed by the technical workbench ([WEBFORM-25](../webforms/WEBFORM-25-technical-workbench.md)), so the same physical card cannot be used twice.
- A copy always shows its item's current details, not what was printed on its card.
- Copies are never deleted.

### Printing

- **Print cards** takes one or more items and a number of copies from 1 to 50, used for every selected item, up to 200 cards per print. It creates the copies and returns one PDF. Archived items cannot be printed.
- The client gives every print an identifier before sending it. Sending the same identifier again with the same items and number of copies returns the same cards and creates nothing new; with different content it is refused.
- **Reprint** prints existing copies again, chosen one by one or as a whole earlier print, with the same QR codes. It creates nothing new, and it works for archived items and deactivated copies.
- **Preview cards** shows the selected items with a sample QR code and saves nothing.
- The cards are rendered by Typst like character cards ([MANAGER-13](MANAGER-13-character-cards.md)): four cards per A4 page, each row one card with the front on the left and the back on the right. The backend ships one default item-card template; administrators can replace it as defined in [SPEC-32](../../SPEC-32-item-card-templates.md).
- **Front:** the type, the name, and the copy's QR code. **Back:** the description, the effect, the charges, and the expiry date when set. Cost, rarity and image are not printed. Labels are localized; user content is printed as entered.

### Lists and rights

- The list shows items 50 at a time, ordered by name, filtered by type (or all types), and searched by text in the name, description and effect (case-insensitive). Lists can show or hide archived items. Changing the filter or the search starts again from the first page.
- Selecting an item shows its fields, its image, its components with their quantities, and its copies grouped by print, each with its QR code, its state, who printed it and when.
- Readers and administrators can list and view everything and preview cards. Only administrators can create, edit, clone, archive, change components, print, reprint, and deactivate or turn on copies, because printing creates copies and a reprint duplicates a valid QR code.
- Every change is an authorized, validated, auditable event-sourced command. Item edits use optimistic concurrency: an edit made from an older version is refused with clear conflict feedback.

## Acceptance criteria

- Administrators can filter by item type, search by text, browse pages, and inspect the components associated with one item.
- Create, edit, and clone workflows cover generic name, type, description/effect, image, cost, charge count, expiry, QR identifier, and component composition as applicable.
- Component additions/removals are validated, confirmed where destructive, and reflected immediately; invalid or duplicate composition is explained.
- Item-card PDFs include the correct item metadata and QR code; multi-copy printing creates the requested number of copies without mutating the item record.
- Search/filter/paging interactions do not silently discard unsaved edits or produce results from a previous filter.
- Item types and rarities can be created, renamed, archived and unarchived; rarities can be reordered.
- Copies can be listed, reprinted, deactivated and turned back on; a repeated print request creates no extra copies.

## Verification

- xUnit tests cover the name, text, cost, charge and expiry rules; unique names; type and rarity rules; component quantity, limit, self-containment, loop and duplicate-recipe rules; rarity order; copy creation, repeated print requests, reprint, deactivation and reactivation; authorization; event emission; and concurrency conflicts.
- Integration tests render a real PDF with the bundled Typst and check its text and page count, and cover image upload, replacement and serving.
- API tests verify discovery, the actions offered to readers and administrators, and stable error codes.
- Management-client tests cover the filters, search, paging and selection, the item dialog (create, edit, clone, image), the component and copy actions with their confirmations, the item type and rarity lists, printing and preview, and English and Italian labels.
