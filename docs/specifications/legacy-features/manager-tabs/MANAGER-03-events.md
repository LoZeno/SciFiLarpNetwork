# Manager tab: Events and participation

**Legacy entry:** `HolonetManager` → “Eventi” (`HolonetManager\Eventi\ControlEventi`).

## Observed behavior

Lists events in pages of 50; selecting an event shows its scheduled days, and selecting one or all days shows registered characters. Administrators can create events, register players, add days, change dates, manage participant points/experience, print participant and advanced-ability cards, and perform those edit operations only while the event is open.


## Product model

Approved 2026-11-21. Characters are defined by [MANAGER-01](MANAGER-01-players-characters.md), their experience and free points by [MANAGER-12](MANAGER-12-character-skills.md), and their cards by [MANAGER-13](MANAGER-13-character-cards.md).

### Events

- An event has a generated identifier, a running number (starting at 1, never reused, from its own sequence), a name, an optional description, optional base event experience points, a status, and its days.
- Names are required and are 1–64 characters after trimming; they are not unique. The description is optional, up to 2,000 characters. Both are user content and are not translated.
- Base event experience points are blank or a whole number of 0 or more. They can be given to participants when the event closes (see Closing).
- The status is **Open** or **Closed**. A new event is Open. Administrators can close an event through the closing grid (see Closing) and reopen it; reopening asks for confirmation.
- While an event is Closed, its name, description, days, registrations, and experience changes cannot change. The management client explains why and disables those actions; the API rejects them with a clear, localized error.
- An event's first and last dates are worked out from its days; they are not stored.
- Nothing is deleted. Closed events can be archived and unarchived. Archived events are hidden from the list by default and clearly marked when shown.

### Days

- A day is a calendar date with optional experience points for that day: blank or a whole number of 0 or more. Dates are unique within an event. An event can have no days.
- Administrators can add a day, change a day's date and experience points, and remove a day that has no registered characters. Once a close has given a day's experience points to anyone, that day cannot be removed and its experience points cannot change; its date still can. The day list shows each day's experience points when set.

### Registrations

- Characters are registered, not players. A character is registered for one or more days of an event.
- Only Active characters can be registered. A registered character stays registered if their status changes later.
- A character is registered on a day at most once. When registering a character already registered on other days of the event, the dialog shows those days and adds only the new ones; asking only for days already registered is rejected.
- Administrators can choose one or more days or all days, and can unregister a character from one or more days while the event is Open. A character with experience changes or closing awards recorded at the event must keep at least one day, so the event keeps showing that history.

### Experience changes

- An administrator can give a registered character a whole-number experience change, positive or negative and not zero, while the event is Open. It changes the character's experience points (MANAGER-01) and is recorded against the event, with the administrator and time.
- The change is rejected when the character's experience points would go below 0 or above 2,147,483,647, or when the character's free points (MANAGER-12) would go below 0.
- The event shows each participant's experience changes and their total.

### Closing

Approved 2026-11-21 as an amendment to this model.

- **Close** opens a grid with one row per registered character and one column per event day, after a column for the base event experience points. The base column is hidden when the event has no base experience points. Readers never see the grid.
- A cell has a checkbox when that experience can be given: the base column always, a day column only when the character is registered for that day and the day has experience points. A day without experience points shows "—"; a day the character is not registered for is empty.
- Checkboxes start ticked, except for characters that are Retired, Dead, or archived: their checkboxes start clear, but they can still be ticked and get the experience.
- One button ticks or clears every day checkbox, and another every base checkbox: when any of them is clear it ticks them all, otherwise it clears them all.
- The grid warns that experience points cannot be changed after closing, so any manual adjustment must be made with **Change experience** before closing.
- **Close** asks to confirm that the selection is final. On confirmation, the event closes and, in the same command, each character gets the sum of the ticked base and day experience points. Each character's award is one entry in the event's experience history, marked as given on closing.
- The close is rejected, and nothing changes, when the selection does not match the event (for example a day the character is not registered for), when an award would take a character's experience points above 2,147,483,647, or when the event changed since the grid was opened. After that last case the grid reloads with the latest data.
- An event can be reopened after closing. On the next close, experience already given shows as **given** and cannot be ticked, so no experience is given twice. A close request for an event that is already Closed is rejected and gives nothing.

### Lists and rights

- Events are listed in pages of 50, newest first day first; events without days come first, then by running number, newest first. Selecting an event shows its days; selecting one day or all days shows the registered characters with their player and days.
- Readers and administrators can browse and print. Only administrators can change events, days, registrations, and experience.

### Printing

- Readers and administrators can preview and export the MANAGER-13 cards of every character registered on one selected day or on the whole event. Advanced skills are already marked on the card (MANAGER-13), so there is no separate advanced-skill card.
- Each card printed from an event carries validity dates: that character's first and last registered day of the event. The card data gains an optional `validity` field; templates that ignore it keep working (SPEC-29). The default template prints it on the back.
- A print request holds at most 200 cards, like MANAGER-13. A larger selection is rejected with a clear, localized error; an event or day with no registrations gives a clear "nothing to print" error.

## Acceptance criteria

- Administrators can browse events, inspect event days, and inspect registrations for one selected day or the whole event.
- Event create/edit, date/day management, player registration, and participant point/experience adjustments are available with explicit validation and confirmation.
- Closed events are read-only for registration, date changes, added days, and point adjustments; the UI explains the restriction and offers no misleading success state.
- Registration prevents duplicate participation for the same character/day and clearly handles a character already registered on other days.
- Participant and advanced-ability printouts reflect the selected event's current registrations and are available as downloadable/printable PDF.
- Paging, empty schedules, no participants, and changes made in dialogs refresh the relevant event/day/participant views.


## Verification

- xUnit tests cover the closing grid rules: base and day experience values, awards of the ticked cells, nothing given twice after a reopen, rejected selections, the stale grid, and the experience maximum.
- xUnit tests cover the name and description rules, experience-change amounts and range, running numbers, edit and stale edit, close, reopen, archive and unarchive, list order and paging, day add, change, and remove rules, registration and unregistration rules, experience changes and their free-point check, the event record of each change, and the conflict between an experience change and a character edit.
- API tests verify discovery, the actions offered to readers and administrators on Open and Closed events, stable error codes, the administrator named on an experience change, and event cards printed with validity dates.
- Management-client tests cover listing, day and participant views, the closed-event explanation, the event, day, registration, and experience dialogs, confirmation, error display, printing, and Italian labels.
