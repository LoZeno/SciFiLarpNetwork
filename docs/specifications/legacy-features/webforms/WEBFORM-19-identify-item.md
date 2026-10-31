# Web feature: Identify an item

**Legacy route:** `IdentificaOggetti\NewIdentificaOggetti.aspx`.

## Observed behavior

Scans a QR GUID and displays the matching item's name, description, effect, type, expiry date, cost, charges, and optional image. Invalid GUIDs and non-item labels have distinct messages.

## Acceptance criteria

- A decoded item code resolves to the item's approved public/character-visible information and never exposes administrative-only fields.
- Invalid, unknown, and wrong-category codes show distinct, recoverable outcomes and clear stale item data.
- Optional image URLs are validated/served safely; missing images do not break the result.
- Users can scan again or use the camera-independent/manual fallback from CROSSCUT-01.
- Text is safely rendered and locale-aware dates/numbers are used.
