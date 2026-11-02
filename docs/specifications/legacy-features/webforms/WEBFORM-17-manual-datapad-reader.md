# Web feature: Manual datapad reader

**Legacy route:** `Holodischi\LetturaDischi.aspx`.

## Observed behavior

Accepts a manually entered datapad access code, lists files on the matching disk, and displays the selected file. The legacy query assumes the code resolves to a disk and does not provide a robust invalid-code result.

## Acceptance criteria

- Users can enter a datapad code and view only files permitted by its configured access rules.
- Unknown, malformed, expired, or unauthorized codes produce a clear generic result and never an unhandled server error.
- Selecting a file displays its title and content safely; returning to the list preserves the selected datapad.
- Access checks occur server-side and are auditable where game rules require it.
- The manual code path remains available as a fallback if QR capture is unavailable.
