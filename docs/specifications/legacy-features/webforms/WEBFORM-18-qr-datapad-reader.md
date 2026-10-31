# Web feature: QR datapad reader

**Legacy route:** `Holodischi\NewLetturaDischi.aspx`.

## Observed behavior

Scans a QR GUID, resolves it to a datapad, lists files with paging, and displays a selected file with a return-to-list action.

## Acceptance criteria

- Scanning a valid datapad code loads only that datapad's files; invalid or non-datapad codes show a recoverable error.
- File selection shows the correct file and a return action restores the selected datapad's list and paging context.
- The user can retry, switch to manual code entry, and continue without camera access using CROSSCUT-01.
- Authorization is evaluated for the current character and datapad at both list and file-read time.
- QR payloads are validated server-side and do not directly encode file contents or sensitive identifiers.
