# Web feature: Legacy mailbox

**Legacy route:** `Messaggi\Messaggi.aspx`.

## Observed behavior

Shows received or sent messages; supports selecting a saved contact as a recipient, composing a title/body to comma-separated character numbers, reading a message, soft-deleting it from the selected mailbox, and saving it to personal files. Messages may be assigned fictional encryption metadata.

## Acceptance criteria

- Users can switch between inbox and sent views, read a message, compose to one or more valid recipients, save a copy to personal files, and hide a message from their own mailbox.
- Recipient resolution is explicit and validated; invalid recipients are reported rather than silently discarded.
- Soft deletion affects only the current character's mailbox entry and does not erase the sender's message or other recipients' copies.
- Read/unread state and message delivery are persisted and reflected consistently.
- Drafts, empty states, duplicate submission, content safety, and authorization are handled.
- This legacy route may redirect to WEBFORM-33's replacement; do not maintain two divergent mailbox implementations.
