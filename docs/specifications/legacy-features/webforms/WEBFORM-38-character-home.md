# Web feature: Character home

**Legacy route:** `Default.aspx`.

## Observed behavior

Looks up the default network title and, when a character is present in session, displays that character's name. Other application navigation is provided by the page master.

## Acceptance criteria

- Authenticated users see their active character identity and only the routes/actions allowed for that character.
- Anonymous users see a useful sign-in/network-selection entry point; missing or expired character context is handled without an exception.
- Home content and navigation are responsive, accessible, and generic/configurable per LARP setting.
- The route does not trust a stale character cookie as authorization; identity is revalidated against the server-side session.
- Available features reflect the character's configured permissions while direct endpoint access is independently authorized.
