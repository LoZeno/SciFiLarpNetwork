# Web feature: Identify a toxin or drug

**Legacy route:** `IdentificazioneTossine\NewIdentificazioneTossine.aspx`.

## Observed behavior

Requires the legacy identification skill (ID 2), scans a substance QR code, and displays metadata for types 0, 2, or 3. Other categories are reported as unsupported.

## Acceptance criteria

- The user's permission/skill is checked server-side on every identification.
- Supported configured toxin/drug/ingredient classes display approved name, description/effect, type, expiry, cost, efficacy, usage, and optional image.
- Unknown, malformed, and wrong-category labels have distinct recoverable states and clear stale results.
- Identification is a fictional game-information function and does not present itself as real-world toxicology advice.
- Provide retry and camera-independent fallback per CROSSCUT-01; validate QR payloads server-side.
