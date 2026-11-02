# Web feature: Configured network sign-in (legacy faction route: Republic)

**Legacy route:** `Login\LoginRepubblica.aspx`.

## Observed behavior

Sets legacy network/faction value 1 and its theme; the route delegates credential entry to shared UI and provides the simulated hacking callbacks.

## Acceptance criteria

- Represent the network through generic configuration rather than franchise-specific branding; any old route is a compatibility alias only.
- Validate the character's network membership server-side before completing authentication.
- A safe, configured theme may reflect the network but cannot grant permissions or bypass login.
- Failed authentication clears the attempted character/network context; successful authentication routes to the shared application home.
- Intrusion is presented only as the fictional challenge specified by CROSSCUT-02.
