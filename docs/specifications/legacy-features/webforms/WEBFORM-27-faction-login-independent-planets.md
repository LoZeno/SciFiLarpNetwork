# Web feature: Configured network sign-in (legacy faction route: FPI)

**Legacy route:** `Login\LoginFPI.aspx`.

## Observed behavior

Sets the network/faction context to legacy value 3, selects its color theme, and exposes the shared simulated hacking flow. The route itself does not implement credential validation.

## Acceptance criteria

- Replace franchise-specific faction identity with a configured generic network login/selection route; retain this route only as a compatibility alias if required.
- A chosen network context is validated against server configuration and used consistently by character authentication and authorization.
- Login success establishes the authenticated character/session; failure does not leave a stale or unauthorized network context.
- Any network-specific visual theme is configurable and does not alter authorization rules.
- Attempting to sign in without network permission is rejected server-side. Do not expose a production simulated-intrusion flow from this sign-in route; any such workflow is deferred to Phase G and must follow CROSSCUT-03.
