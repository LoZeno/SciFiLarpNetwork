# Web feature: Network news feed

**Legacy route:** `Notizie\Notizie.aspx`.

## Observed behavior

Loads active news for the current network and current time, then displays stories sequentially with previous/next navigation, author/date/title/body, and a randomly selected reporter image.

## Acceptance criteria

- The feed displays only stories active for the selected network and configured current time, ordered consistently and deterministically.
- A user can navigate previous/next without moving outside the available story range; no-news and single-story states are clear.
- Author, publication date, title, body, and optional generic setting-specific media are shown with accessible text alternatives.
- Refreshing or paging does not unexpectedly reorder the feed; seeded randomness is not used for content identity or authorization.
- Story content is rendered safely and network scope is checked server-side.
