# Web feature: QR sample medical analysis

**Legacy route:** `AnalisiMedica\NewAnalisiMedica.aspx`.

## Observed behavior

Requires the medical-analysis skill (legacy ID 3). Scans a QR GUID for a character/sample, shows a 30-second in-game processing state, then lists that character's conditions or reports an unknown/non-blood sample. Includes a reset-to-scan action.

## Acceptance criteria

- Authorized users can start a scan, cancel/reset it, and see an accessible processing state and result.
- The decoded opaque identifier is validated server-side and resolves only an eligible sample/character within the configured setting.
- Results distinguish a valid healthy sample, one or more configured conditions, invalid/non-sample identifiers, and unavailable data without exposing unrelated character records.
- Access is checked on every request; a crafted request cannot bypass the character skill/permission requirement.
- The scan works with camera permission denied/unavailable through the QR fallback in CROSSCUT-01 and allows restarting without stale results.
- Simulated delay is configurable and server-controlled; client timer completion alone cannot manufacture a successful result.
