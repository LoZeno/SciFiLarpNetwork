# Web feature: Mailbox and messaging

**Legacy route:** `Messaggi\NewMessaggi.aspx`.

## Observed behavior

Displays paged incoming/outgoing message lists, marks a selected message read, shows sender/title/date/body and sent-message recipients, supports composing to saved contacts or entered names/IDs, replying to an incoming message, saving to personal files, soft-deleting a mailbox copy, and sending with optional fictional encryption.

## Acceptance criteria

- An authenticated character can switch between inbox and sent messages, page results, and read a selected message with accurate sender/recipient/time/read state.
- Compose supports one or more validated recipients (including contact selection), a title, body, and optional in-game encryption metadata; the sender receives an outgoing record and each recipient receives an incoming record.
- Reply pre-fills the source sender and subject context but remains editable before send.
- Save-to-files creates one owned copy; soft delete affects only the current mailbox entry and requires clear success/error feedback.
- Sending, replying, saving, and deleting are idempotent on retry, and all operations enforce ownership and recipient rules server-side.
- Message body is rendered safely; encrypted display is explicitly fictional unless independently secured cryptography is implemented.
- Inbox/outbox empty states, invalid recipients, long messages, refresh, and navigation are accessible and preserve unsent drafts.
