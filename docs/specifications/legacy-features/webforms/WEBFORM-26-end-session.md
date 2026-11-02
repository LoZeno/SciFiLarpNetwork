# Web feature: End session

**Legacy route:** `Login\EndSession.aspx`.

## Observed behavior

Clears the logged character and hacking theme, then redirects to the home page.

## Acceptance criteria

- Logout invalidates the server-side authenticated session, character context, breach context, and any sensitive client session tokens.
- The browser is redirected to a safe configured landing/sign-in page and protected back-navigation cannot restore authenticated content.
- Logout is idempotent and succeeds even if the session has already expired.
- The application distinguishes application logout from other characters' sessions and does not clear unrelated game data.
