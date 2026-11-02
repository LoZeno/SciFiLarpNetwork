# Web feature: Compromised contacts

**Legacy route:** `Hacking\HackedContacts.aspx`.

## Observed behavior

The page code-behind is empty; it currently defines no data load, access check, or interaction.

## Acceptance criteria

- Do not infer functional behavior from the route name. Implement only if the approved game scope includes viewing a breached character's contacts.
- If implemented, require a successful category-specific simulated breach and show only the target's contacts, not the hacker's contacts.
- Clearly distinguish read-only viewing from copying/importing contacts; any copy action requires an explicit game rule and audit event.
- Empty results, denied access, and expired breach context are handled explicitly.
- If omitted, remove the route from the menu and provide a clear unsupported-feature response rather than an empty screen.
