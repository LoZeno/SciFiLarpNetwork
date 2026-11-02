# Web feature: Configured network sign-in (legacy faction route: Empire)

**Legacy route:** `Login\LoginImpero.aspx`.

## Observed behavior

Sets legacy network/faction value 2 and a corresponding theme; the route itself has no credential-validation logic and uses the shared hackable-page contract.

## Acceptance criteria

- Replace the franchise-specific network with a generic configured network route, or retain this only as a documented migration alias.
- The selected network is checked against server configuration and character authorization before establishing a session.
- Theme/branding is data-driven and is not used as a security boundary.
- A failed/unauthorized sign-in clears the attempted identity and network context and shows a recoverable message.
- Shared challenge UI obeys CROSSCUT-02 and executes no real hacking activity.
