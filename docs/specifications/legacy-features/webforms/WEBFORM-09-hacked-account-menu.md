# Web feature: Compromised account menu

**Legacy route:** `Hacking\HackedAccount.aspx`.

## Observed behavior

After an account is breached, displays the target owner and offers categories for news, incoming/outgoing messages, contacts, and files. The code configures difficulty and optional override-code eligibility per category, then routes a successful attempt to a category-specific view. Contacts currently has no implemented click behavior.

## Acceptance criteria

- A successful simulated breach opens a target-scoped menu listing only the categories the game configuration permits the character to access.
- The UI displays target identity and category names without exposing data until the category authorization/challenge succeeds.
- Per-category protection, optional game override item/code rules, and access outcome are computed and validated server-side.
- Unimplemented categories are either completed with their own acceptance criteria or hidden/disabled with a clear status; an empty button must not imply a functioning feature.
- Leaving the breached-account flow clears target context and returns the user safely to their own account.
- The interaction follows CROSSCUT-02 and performs no real-world intrusion, command execution, or network scanning.
