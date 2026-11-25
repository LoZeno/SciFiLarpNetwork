# Manager tab: Datapads and files

**Legacy entry:** `HolonetManager` → “DataPad & Holodisk” (`HolonetWinControls\Holodischi\ControlHolodischi`).

## Observed behavior

Lists searchable/paged datapads (legacy holodisks), displays files belonging to a selected datapad, and supports creating/editing datapads, adding/editing files, and printing QR datapad cards. A single selected datapad is repeated for multiple copies in the print flow.

## Acceptance criteria

- Administrators can browse/search datapads and inspect their files.
- Create/edit workflows manage datapad identity, access code/QR identifier, and associated file title/content using the approved generic domain vocabulary.
- Administrators can add and edit files on the selected datapad; changes remain scoped to that datapad and show clear validation and persistence status.
- Printable datapad cards contain the correct identity/QR data; repeated print copies do not create extra datapads.
- Empty results, missing files, long content, and save failures are handled without stale or misleading detail views.
- Pictures and videos used by datapad files are stored in their own subfolder of the media folder and are shared with other features, as decided in [SPEC-30](../../SPEC-30-uploaded-file-storage-review.md) (decisions 1 and 4). This row builds the shared media library: one backend library for all features, content pointing at media by identifier (`media:<id>`), a Management app media dialog listing every item with a filter by origin, and removal refused while any feature's current content uses the item. News moves to that shared library in the same row.
