# Manager tab: Event print management

**Legacy entry:** `HolonetManager` → “Gestione Stampe Eventi” (`HolonetWinControls\Eventi\ControlStampeEventi`).

## Observed behavior

Lists events in pages of 50 and shows printable items and datapads associated with a selected event. Administrators can add item/substance/ingredient or datapad print entries, remove entries by setting their print copies to zero, and generate separate PDFs for objects, substances, ingredients, and datapads.

## Acceptance criteria

- Selecting an event displays all configured printable item, substance, ingredient, and datapad entries with requested copy counts.
- Administrators can add entries and set or remove their copy counts with validation, confirmation, and clear update status.
- Printing generates correctly categorized downloadable PDFs for non-empty categories, using current event assignments and requested quantities.
- An empty category produces no misleading “printed” result; partial print failure identifies which files were successfully created.
- Event paging and selection do not mix entries between events; print operations are repeatable and do not alter source catalog records.
