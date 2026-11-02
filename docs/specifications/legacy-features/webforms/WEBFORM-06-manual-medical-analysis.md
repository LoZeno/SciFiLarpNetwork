# Web feature: Manual medical analysis

**Legacy route:** `AnalisiMedica\AnalisiMedica.aspx`.

## Observed behavior

Requires the legacy medical-analysis ability (skill ID 3), accepts a manually entered enzyme code, simulates a 30-second analysis, and displays the linked infection name/description or an unrecognized-enzyme result.

## Acceptance criteria

- Authorized medical characters can enter or scan a supported sample identifier and receive an analysis result after a configurable in-game delay.
- Unauthorized users cannot invoke the analysis endpoint by bypassing the UI.
- Recognized and unrecognized codes produce distinct, understandable results; results are based on configured sample/catalog data, not client-supplied diagnosis text.
- The generic product supports configurable medical-condition names and descriptions and makes no real-world medical diagnosis claim.
- The legacy manual enzyme-code path is either retained as an explicit fallback or replaced by the QR sample flow in WEBFORM-07; do not silently omit both.
