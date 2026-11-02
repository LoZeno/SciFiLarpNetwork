# Manager tab: Items and components

**Legacy entry:** `HolonetManager` → “Oggetti & Componenti” (`HolonetWinControls\Oggetti\ControlOggetti`).

## Observed behavior

Filters items by type, provides paged listing and text search, displays components for a selected item, and supports create, edit, clone, add/remove component, and printable item-card PDF generation. A single selected item can optionally be repeated for multiple physical copies.

## Acceptance criteria

- Administrators can filter by item type, search by text, browse pages, and inspect the components associated with one item.
- Create, edit, and clone workflows cover generic name, type, description/effect, image, cost, charge count, expiry, QR identifier, and component composition as applicable.
- Component additions/removals are validated, confirmed where destructive, and reflected immediately; invalid or duplicate composition is explained.
- Item-card PDFs include the correct item metadata and QR code; multi-copy printing creates the requested number of copies without mutating the item record.
- Search/filter/paging interactions do not silently discard unsaved edits or produce results from a previous filter.
