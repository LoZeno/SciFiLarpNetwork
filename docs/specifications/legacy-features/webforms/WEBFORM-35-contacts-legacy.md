# Web feature: Legacy contacts list

**Legacy route:** `Messaggi\Rubrica.aspx`.

## Observed behavior

Lists contacts for the signed-in character and allows adding a character number/display-name pair. The code-behind has no delete action.

## Acceptance criteria

- Route to the full contacts feature in WEBFORM-34 or remove it; do not preserve a second, reduced implementation.
- If retained as a compatibility route, existing contact data is presented with the same ownership, validation, and authorization rules as WEBFORM-34.
- Add flow reports invalid/duplicate contacts instead of silently ignoring them.
- Any compatibility redirect preserves only safe navigation state and does not leak contact identifiers in public URLs.
