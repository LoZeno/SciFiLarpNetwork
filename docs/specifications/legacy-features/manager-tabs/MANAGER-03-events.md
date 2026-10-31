# Manager tab: Events and participation

**Legacy entry:** `HolonetManager` → “Eventi” (`HolonetManager\Eventi\ControlEventi`).

## Observed behavior

Lists events in pages of 50; selecting an event shows its scheduled days, and selecting one or all days shows registered characters. Administrators can create events, register players, add days, change dates, manage participant points/experience, print participant and advanced-ability cards, and perform those edit operations only while the event is open.

## Acceptance criteria

- Administrators can browse events, inspect event days, and inspect registrations for one selected day or the whole event.
- Event create/edit, date/day management, player registration, and participant point/experience adjustments are available with explicit validation and confirmation.
- Closed events are read-only for registration, date changes, added days, and point adjustments; the UI explains the restriction and offers no misleading success state.
- Registration prevents duplicate participation for the same character/day and clearly handles a character already registered on other days.
- Participant and advanced-ability printouts reflect the selected event's current registrations and are available as downloadable/printable PDF.
- Paging, empty schedules, no participants, and changes made in dialogs refresh the relevant event/day/participant views.
