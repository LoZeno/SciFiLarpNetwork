# Web feature: QR login prototype

**Legacy route:** `Login\NewLogin.aspx`.

## Observed behavior

Initializes the default network/theme and attaches a QR decode callback, but the callback contains only a placeholder and does not authenticate a user. The functional QR login is implemented in the separate `QRLoginUserControl`.

## Acceptance criteria

- Do not treat the prototype as a working login flow; either retire it or complete it through the approved identity design.
- If QR login is retained, scanned credentials are high-entropy, revocable, purpose-limited tokens; the QR payload is not a public character GUID or password substitute.
- Server-side validation enforces network membership, token expiry/revocation, replay protection, and rate limits before establishing a session.
- Camera permission is optional; provide manual/other sign-in fallback, and never expose QR secrets in URLs, logs, analytics, or rendered HTML.
- The implementation uses CROSSCUT-01 for camera capture and does not retain camera frames.
