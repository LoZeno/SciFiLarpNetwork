# Manager tab: Messages

**Legacy entry:** `HolonetManager` → “Messaggi” (`HolonetWinControls\Messaggi\ControlMessaggi`).

## Observed behavior

Lists messages in pages of 50. The administrator can show all messages or choose a character to inspect that character's latest incoming message. Selecting a message shows its text and recipients. Dialogs support creating a message and replying to a selected message.

## Acceptance criteria

- Administrators can page through all messages or filter to a character's incoming messages, and view message details, sender, recipients, and timestamps.
- Message creation and reply validate sender, at least one valid recipient, title, and content, then show a persisted result or a specific validation error.
- A reply is linked to the source message where appropriate and does not accidentally duplicate recipients or send twice on repeated submission.
- Administrative visibility and any ability to inspect message content are permission-controlled and auditable.
- The UI handles empty lists, removed characters, large recipient lists, and long message bodies without corrupting content.
