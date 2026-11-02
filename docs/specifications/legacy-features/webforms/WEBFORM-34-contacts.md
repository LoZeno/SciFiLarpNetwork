# Web feature: Contacts

**Legacy route:** `Messaggi\NewRubrica.aspx`.

## Observed behavior

Lists a character's contacts with paging, supports adding a contact by character number and display name, and deleting selected entries.

## Acceptance criteria

- A character can list, search/page, add, rename, and remove only their own contacts.
- New contact targets must resolve to valid characters according to configured visibility rules; duplicate targets are prevented or handled explicitly.
- Deletion is confirmed and removes only the contact association, never the target character.
- Message composition can select a contact and resolves to its current character identity.
- Invalid identifiers, empty names, stale target records, and persistence failures have actionable feedback.
