# Manager tab: News

**Legacy entry:** `HolonetManager` → “Notizie” (`HolonetWinControls\Notizie\ControlNotizie`).

## Observed behavior

Selects a network/faction and a display date, then lists that network's news for that date or all dates. Selecting a story displays its author and body. Dialogs create and edit stories.

## Acceptance criteria

- Administrators can filter stories by configured network and either active-at-date or all-date mode.
- A story has a title, body, author, network, publication/start time, and end/expiry time; fields are validated, including end-after-start.
- Administrators can create and edit stories with a visible preview and clear persistence feedback.
- Public/client news views show only stories active for the selected network and time; administrative all-date mode can still inspect expired and scheduled stories.
- Date/time and timezone behavior is explicit and consistent between the editor, client, and stored event history.
