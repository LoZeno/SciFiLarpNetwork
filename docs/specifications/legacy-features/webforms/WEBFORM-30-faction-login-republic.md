# Web feature: Configured network sign-in (legacy faction route: Republic)

**Legacy route:** `Login\LoginRepubblica.aspx`.

## Observed behavior

Sets legacy network/faction value 1 and its theme; the route delegates credential entry to shared UI and provides the simulated hacking callbacks.

## Acceptance criteria

- Represent the network through generic configuration rather than franchise-specific branding; any old route is a compatibility alias only.
- Validate the character's network membership server-side before completing authentication.
- A safe, configured theme may reflect the network but cannot grant permissions or bypass login.
- Failed authentication clears the attempted character/network context; successful authentication routes to the shared application home.
- Do not expose a production simulated-intrusion flow from this sign-in route; any such workflow is deferred to Phase G and must follow CROSSCUT-03.
