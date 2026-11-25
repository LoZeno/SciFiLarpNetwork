# SPEC-30: Uploaded file storage review

## Status

Assessment, written 2026-11-24 after [MANAGER-05](legacy-features/manager-tabs/MANAGER-05-news.md). It is row 31 of [SPEC-22](SPEC-22-implementation-order.md). The user decided on 2026-11-25; the decisions are under [Decisions](#decisions). The approved code change is [SPEC-31](SPEC-31-media-folder-layout.md), row 32 of SPEC-22.

## Question

MANAGER-05 stores news pictures and videos as files in a backend folder (`Media:RootPath`), with only their facts in the event history. Features delivered earlier store uploaded bytes inside the database as Marten documents. Should those earlier features move to the folder, so the product has one way to keep uploaded files?

## Current state

| Feature | What is uploaded | Largest | Stored in | Served by |
|---|---|---|---|---|
| Networks ([MANAGER-11](legacy-features/manager-tabs/MANAGER-11-networks.md)) | Background image (PNG, JPEG, WebP) | 2 MB | `NetworkImageDocument`, bytes in `jsonb` as base64 | `/api/v1/network-images/{id}`, whole file |
| Card templates ([SPEC-29](SPEC-29-card-templates.md)) | Typst template source | 256 KB | `CardTemplateContentDocument`, bytes in `jsonb` as base64 | Read whole to render cards and to download the source |
| News ([MANAGER-05](legacy-features/manager-tabs/MANAGER-05-news.md)) | Pictures and videos | 5 MB / 100 MB | Files in `Media:RootPath`, named by identifier | `/api/v1/news-media/{id}/content`, with range requests |

In every case the event history records only an identifier, a type, and a size, never the bytes, and stored bytes are never changed. Network images and card templates are kept after they are replaced, so history keeps its references; news media is deleted when removed, because no story uses it any more.

## Why news uses files

These reasons come from the MANAGER-05 decision (approved 2026-11-24). They are the measure for the earlier features.

1. Size: a `jsonb` document holds the bytes as base64, about a third larger, and loads them whole into memory. That is fine for a few megabytes, not for 100 MB videos.
2. Volume: 20–50 stories a day can add about 1 GB of video per event.
3. Range requests: browsers fetch videos in parts to jump ahead. ASP.NET Core serves parts of a file or seekable stream directly; serving parts of a `jsonb` value is not practical.

## Assessment

| Measure | Network images | Card templates |
|---|---|---|
| Size per file | Small (≤ 2 MB) | Tiny (≤ 256 KB) |
| How many | One current image per network; a few networks | One current template, plus replaced ones |
| Needs range requests | No: shown whole | No: read whole |
| Read path | Management app and, later, the player screens ([WEBFORM-39](legacy-features/webforms/WEBFORM-39-network-selection.md)) | Card rendering and source download |
| Gain from moving | One storage model; smaller database | One storage model |
| Cost of moving | New write path with file-then-event ordering and clean-up; a migration of existing documents; tests | Same, plus the renderer reads the file instead of a document |

### What the database gives today

- One backup: a database dump holds the event history and every uploaded byte together, so a restore can never point at a missing file.
- One transaction: the network image document and its event commit together; the file approach needs the file written first and deleted when the event does not commit (see `NewsMediaService`).
- No folder permissions, disk paths, or second volume to configure.

### What the database costs

- Database size and dump time grow with every replaced image or template, since they are kept. With a handful of networks and one template, this stays in the low tens of megabytes.
- Two storage models to explain to operators and to back up correctly.

## Decisions

Made by the user on 2026-11-25. The first draft recommended keeping network images in the database; the user chose otherwise, because the media folder must be backed up anyway now, so the database no longer gives "one backup", and network images will be served to players ([WEBFORM-39](legacy-features/webforms/WEBFORM-39-network-selection.md)).

1. **Pictures and videos live in the media folder** (`Media:RootPath`), one subfolder per feature:

   | Feature | Subfolder | Delivered by |
   |---|---|---|
   | News media | `News/` | [SPEC-31](SPEC-31-media-folder-layout.md) (moves from the folder root) |
   | Network background images | `NetworkImages/` | [SPEC-31](SPEC-31-media-folder-layout.md) (moves from the database) |
   | Datapad media | Its own subfolder | [MANAGER-08](legacy-features/manager-tabs/MANAGER-08-datapads-files.md) |
   | Personal file media | Its own subfolder | [WEBFORM-08](legacy-features/webforms/WEBFORM-08-personal-files.md) |

   Every later picture or video upload follows the same rule.
2. **Card templates stay in the database.** They are Typst source, not pictures; only the Management app and the card renderer read them, and the backend never serves them to the web client.
3. **Files are named by identifier and never renamed or overwritten.** Each upload gets a new identifier. Replaced network images are kept, so history keeps its references; the upload time is in the event, not the file name. News media keeps its rule: a file is deleted only when it is removed and nothing uses it.
4. **Media is shared across features.** Content points at media by identifier (`media:<id>`), never by folder, so a news story can use a picture uploaded for a datapad, and the reverse. The subfolder records where an item was uploaded; it does not limit where it is used. This needs one backend media library for all features, a Management app media dialog that lists every item with a filter by origin, and removal refused while any feature's current content uses the item. It is built with MANAGER-08, the first feature that needs it. Network images stay outside the shared library, because they are a network setting, not content.
5. **Backup rule, added now** to [SPEC-09](SPEC-09-event-store-and-projections.md) and [SPEC-18](SPEC-18-self-hosted-deployment.md): back up and restore the database and `Media:RootPath` together, and the restore test checks that every media item recorded in the database has its file.
6. **No data move.** No installation has been deployed, so SPEC-31 resets development data instead of migrating it.

## Acceptance criteria

- The user has chosen a direction for each existing upload feature, and the choice is recorded here.
- Any approved migration has its own SPEC-22 row, specification, and tests, including moving existing data without losing history.
- Deployment and backup documentation names every place uploaded bytes are kept.
