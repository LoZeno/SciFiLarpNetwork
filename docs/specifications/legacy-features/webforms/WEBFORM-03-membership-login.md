# Web feature: Membership login

**Legacy route:** `Account\Login.aspx`.

## Observed behavior

Hosts the standard ASP.NET membership login control and links to registration while preserving a return URL.

## Acceptance criteria

- Users can authenticate through the configured supported identity flow and receive a session with least-privilege access.
- A return destination is accepted only when it is a safe local destination; failed authentication gives a generic, accessible error.
- The page links to registration and returns users to their intended page after successful authentication when safe to do so.
- Session renewal, logout, expiration, and credential-reset behaviors are documented and consistent across clients.
