# Web feature: Default network sign-in

**Legacy route:** `Login\LoginHolonet.aspx`.

## Observed behavior

Clears the session, sets the legacy default network value 0 and theme, and exposes the shared hacking challenge. It does not itself authenticate a character.

## Acceptance criteria

- The generic default network is configurable and selected without franchise-specific naming.
- Selecting this route initializes only the intended network/login context; it cannot accidentally preserve another character's session or breach state.
- Character sign-in is performed by the shared supported credential or QR flow and checks membership/authorization for the selected network.
- Successful and failed authentication have clear, accessible outcomes and safe redirection.
- This sign-in route does not initiate simulated intrusion. Any later in-game intrusion workflow is separate from authentication and follows CROSSCUT-03.
