# Manager tab: Messages

**Legacy entry:** `HolonetManager` → “Messaggi” (`HolonetWinControls\Messaggi\ControlMessaggi`).

## Observed behavior

Lists messages in pages of 50. The administrator can show all messages or choose a character to inspect that character's latest incoming message. Selecting a message shows its text and recipients. Dialogs support creating a message and replying to a selected message.

## Product model

Approved 2026-11-23. Characters are defined by [MANAGER-01](MANAGER-01-players-characters.md). Per-character mailbox state (read, deleted, saved to personal files) and fictional encryption are added by [WEBFORM-33](../webforms/WEBFORM-33-messages.md); this tab does not create them.

### Messages

- A message has a generated identifier, a sender, one or more recipients, a title, a body, the time it was sent, and, for a reply, the message it replies to. A sent message never changes and is never deleted.
- The sender is a character. Administrators choose it; an in-game sender without a player uses a non-player character.
- Recipients are characters. A recipient listed twice is kept once, and a message holds at most 200 recipients. The sender can also be a recipient.
- The sender and every recipient must be existing characters that are not archived. Their status does not matter: Active, Retired, and Dead characters can send and receive.
- Titles are required, 1–128 characters after trimming. Bodies are required, up to 10,000 characters, must not be only whitespace, and must not contain the null character (U+0000), which the database cannot store; the body is stored exactly as typed, including line breaks and tabs. Both are user content and are not translated.
- The sending administrator and the time are recorded with the message.

### Replies

- An administrator can reply to any message. The reply dialog starts with the source message's first recipient (or the character whose incoming messages are listed) as sender, the source sender as recipient, and the title with a `Re: ` prefix unless it already has one; every field can be changed before sending.
- A reply records the message it replies to. The source message must exist. The message details show the source message's title and let the administrator open it.

### Repeated submission

- The dialog gives every new message an identifier before sending. Sending the same identifier again returns the message already sent and sends nothing new, so a repeated click or a retry after a lost answer never sends twice. Reusing an identifier for different content is rejected.

### Lists and rights

- Messages are listed in pages of 50, newest first. The list shows the sender, the number of recipients, the title, and the time; it never shows the body.
- The list shows all messages, or the incoming messages of one chosen character.
- Approved 2026-11-23 as an amendment: the list updates in real time. When a message is sent, every open messages screen that lists all messages, or the incoming messages of one of its recipients, reloads the page it shows. It keeps that page, the selected message, and its open details. The update names only the message and its recipients, never the title or text, and does not count as an opening. A lost connection keeps reconnecting while the screen is open, and the screen says the updates are interrupted; when it comes back the note goes away and the page is reloaded once, because updates sent meanwhile are lost. A live update that arrives while the user is changing page or filter waits for that change, so it never takes the user back. If live updates are not available, or stop for good because the session is refused, the screen says so, still works, and tries again the next time it loads.
- Characters are shown with their running number and current name. Archived characters are marked; a character that cannot be found is shown as unknown, and the message still opens.
- Readers and administrators can list messages and open one to see its sender, every recipient, title, body, time, sending administrator, and source message. Every opening of a message's details is recorded with the account and time. Sending is not an opening: the sending administrator sees the sent message without it being recorded, so the record lists only deliberate inspections. Only administrators can send messages and replies.

## Acceptance criteria

- Administrators can page through all messages or filter to a character's incoming messages, and view message details, sender, recipients, and timestamps.
- Message creation and reply validate sender, at least one valid recipient, title, and content, then show a persisted result or a specific validation error.
- A reply is linked to the source message where appropriate and does not accidentally duplicate recipients or send twice on repeated submission.
- Administrative visibility and any ability to inspect message content are permission-controlled and auditable.
- The UI handles empty lists, removed characters, large recipient lists, and long message bodies without corrupting content.


## Verification

- xUnit tests cover the sender and recipient rules (unknown, archived, any status), duplicate recipients, the recipient limit, title and body rules, replies and their source, repeated submission with the same and with different content, list order, paging, the incoming filter, and the record of every opening.
- API tests verify discovery, the actions offered to readers and administrators, stable error codes, the sending administrator, and that readers cannot send.
- Integration tests cover the live updates: the collection's link, a reader receiving each sent message with its recipients only, an anonymous connection being refused, and the connection closing when the session expires. Management-client tests cover reloading the page shown for every message or only for the listed character, keeping the page and the open message, reloading after a reconnect, the interrupted and unavailable notes, listening again after the connection stopped for good, a live update during a page change, the reconnect delays and when they stop, stopping on leaving the screen or signing out, and the note when live updates are unavailable.
- Management-client tests cover listing, paging, the character filter, the empty list, the details view with archived and unknown characters, the message and reply dialogs, error display, and Italian labels.
