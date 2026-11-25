# Manager tab: News

**Legacy entry:** `HolonetManager` → “Notizie” (`HolonetWinControls\Notizie\ControlNotizie`).

## Observed behavior

Selects a network/faction and a display date, then lists that network's news for that date or all dates. Selecting a story displays its author and body. Dialogs create and edit stories.

The legacy story also held a hacking level. Its body was raw HTML: administrators copied pictures and videos into a folder of the web application and wrote `<img>` and `<video>` tags by hand.

## Product model

Approved 2026-11-24. Networks are defined by [MANAGER-11](MANAGER-11-networks.md) and characters by [MANAGER-01](MANAGER-01-players-characters.md). The player feed is [WEBFORM-36](../webforms/WEBFORM-36-news-feed.md); in-game editing after a challenge is [WEBFORM-14](../webforms/WEBFORM-14-hacked-news.md).

### Stories

- A story has a generated identifier, a network, an author, a title, a body, a start time, an end time, and a hacking difficulty.
- Titles are required, 1–128 characters after trimming, without control characters.
- The author is a character. When a story is created, or its author changes, the author must exist and not be archived; status does not matter. An edit may keep an author archived since.
- The network must exist and be enabled when a story is created or moved to it. An edit may keep a network disabled since.
- The hacking difficulty is a whole number of 1 or more, with no maximum, and defaults to 1. It is the difficulty a challenge against the story uses ([SPEC-26](../../SPEC-26-hacking-game-settings.md)); nothing in this tab uses it.
- Titles and bodies are user content and are not translated.

### Times

- Start and end are exact instants, stored in UTC. The end must be after the start.
- A story is active from its start time up to, but not including, its end time.
- The Management app enters and shows times in the computer's time zone, to the minute, and names that time zone next to the fields. Other clients convert the stored instants to their own time zone the same way.

### Body

- The body is Markdown (CommonMark), stored exactly as typed: required, up to 10,000 characters, not only whitespace, and without U+0000.
- The editor's toolbar offers bold, italic, headings, bulleted and numbered lists, and inserting a picture or video. Other Markdown is kept as typed.
- Raw HTML is never rendered: it is shown as text. Links are shown as text and never open.
- A picture or video is written as `![text](media:<media id>)`. The text is required and is shown to people who cannot see the media. A video may add its options: `media:<media id>?autoplay&loop&controls`, in any order, each at most once. Options are refused on a picture.
- Every image in the body must name a media item of the library that is not removed. Images from any other address are refused, because the application never loads anything from outside the venue network.
- A body uses at most 10 pictures and videos.
- An autoplaying video always starts muted, because browsers block autoplay with sound. A player can turn the sound on when controls are shown.

### Media library

- Pictures and videos are uploaded once to a shared library and can be used in many stories.
- A media item has a generated identifier, a name, a media type, a size in bytes, the upload time, and the uploading administrator. Names are required, 1–128 characters after trimming, and are not unique.
- Pictures are PNG, JPEG, WebP, or GIF (animated GIFs allowed), up to 5 MB and 4096 × 4096 pixels. Videos are WebM or MP4, up to 100 MB. SVG is refused, because it can run scripts.
- The server finds the type, and for pictures the pixel size, from the file's first bytes, never from its name or a declared type.
- The files are kept on the backend server, in the `News/` subfolder of the media folder set by `Media:RootPath` (default: `media` next to the backend's own files), named by their identifier. Events record only the identifier, name, type, and size. Backups must copy this folder as well as the database.
- The backend serves media itself, with the stored type, `X-Content-Type-Options: nosniff`, and support for requesting parts of a file, so a video can jump ahead. The web application never serves the folder directly.
- A media item can be removed only when no story's current body uses it. Removing deletes the file; the history keeps the record.
- The Management app shows pictures. A video is shown as a video icon with its name, type, and size, because playing it would need a native video library on every platform.
- The Management app states the picture and video size limits wherever media is uploaded.

### Creating and editing

- The editor gives every new story an identifier before saving. Saving the same identifier again with the same content returns the story already saved and creates nothing new; with different content it is refused.
- Every field can be edited. Each edit records all the new values in one event, with the administrator and the time. An edit made from an older version than the stored one is refused with `errors.concurrentChange`, and nothing is overwritten. The dialog keeps the typed values and says so; saving again is a deliberate choice to replace the other change.
- Stories are never deleted. To withdraw a story early, move its end time.
- The editor shows a preview of the story as players will see it.

### Lists and rights

- The list shows one network's stories, either those active at a chosen date and time (default: now), or all of them. Stories are listed in pages of 50, latest start first.
- Each row shows the title, author, start, and end, and is marked **Scheduled**, **Active**, or **Expired** compared with the current time.
- The details show the title, author, network, start, end, hacking difficulty, body as players will see it, and who last saved the story and when. Characters are shown with their running number and current name; archived characters are marked, and one that cannot be found is shown as unknown.
- Readers and administrators can list and view stories and the media library. Only administrators can create and edit stories and upload or remove media. Views are not recorded.
- The backend's query for stories active on a network at a time is the one the player feed will use; the player screen and its access to media are delivered with WEBFORM-36.

## Acceptance criteria

- Administrators can filter stories by configured network and either active-at-date or all-date mode.
- A story has a title, body, author, network, publication/start time, and end/expiry time; fields are validated, including end-after-start.
- Administrators can create and edit stories with a visible preview and clear persistence feedback.
- Public/client news views show only stories active for the selected network and time; administrative all-date mode can still inspect expired and scheduled stories.
- Date/time and timezone behavior is explicit and consistent between the editor, client, and stored event history.

## Verification

- xUnit tests cover the title, body, time, difficulty, and media-name rules; active and status times at the start and end; times stored in UTC to the microsecond; picture and video detection from the first bytes (PNG, JPEG, WebP, GIF, WebM, MP4; SVG, QuickTime, and Matroska refused) and the size limits; and the Markdown checks: library images only, reference-style images, text required, video options, raw HTML as text.
- Integration tests cover creation, edits, stale edits, repeated submission, unknown and disabled networks, unknown and archived authors (kept on an edit), the active-at and all lists, paging, media in the body, upload, removal of unused and used media, files left behind by refused uploads, and a removal racing a story's first use of the media, which never both succeed.
- API tests verify discovery, the actions offered to readers and administrators, stable error codes, the saving administrator, `multipart/form-data` upload, the served type with `nosniff`, and range requests.
- Management-client tests cover the network and mode filters, local times sent in UTC, paging, details and the preview with pictures and videos, the story dialog (defaults, saving, repeated identifier, errors, the edit of a story on a disabled network, the concurrent-change case, toolbar, media insertion), the media library (limits, upload, removal, choosing media), and English and Italian labels.
- The body is parsed with Markdig 1.6.0 (BSD-2-Clause, managed code, no network access), shared by the backend and the Management app through `Shared/SciFiLarpNetwork.NewsMarkdown`.
