# Web feature: Identify a substance or ingredient

**Legacy route:** `IdentificazioneSostanze\NewIdentificazioneSostanze.aspx`.

## Observed behavior

Requires the legacy identification skill (ID 1), scans a substance QR code, and displays metadata for substance types 0 or 1. Other categories are rejected with an explanatory message.

## Acceptance criteria

- The user's permission/skill is checked server-side before each identification request.
- Valid labels in the configured supported categories show name, description/effect, type, expiry, cost, efficacy, usage instructions, and optional image.
- Unknown labels, invalid codes, and labels of unsupported categories produce distinct safe results without retaining details from a prior scan.
- Category membership and displayed fields are configured domain rules, not client-controlled filters.
- Retry and manual fallback are available if camera access is denied or unavailable, as specified by CROSSCUT-01.
