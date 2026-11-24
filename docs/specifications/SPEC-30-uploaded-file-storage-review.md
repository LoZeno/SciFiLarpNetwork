# SPEC-30: Uploaded file storage review

## Status

Assessment, written 2026-11-24 after [MANAGER-05](legacy-features/manager-tabs/MANAGER-05-news.md). It is row 31 of [SPEC-22](SPEC-22-implementation-order.md): the row is done when the user has made the decisions below and they are recorded here. Nothing in the code changes until then, and any approved migration gets its own SPEC-22 row.

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

## Recommendation

Not documented; my inference from the table above, for the user to decide.

- **Keep network images and card templates in the database for now.** They are small, few, read whole, and never need range requests. Moving them adds a migration and a riskier write path for little gain.
- **Use the media folder for new uploads that are large or need range requests**, such as videos, and for player-facing media served often. Decide each new upload feature with the three measures above (size, volume, range requests) and record the choice in its specification.
- **Make the backup rule explicit now.** The backup and restore procedures that [SPEC-09](SPEC-09-event-store-and-projections.md) requires, delivered with [SPEC-18](SPEC-18-self-hosted-deployment.md) (self-hosted deployment), must back up and restore the database and `Media:RootPath` together, and their restore test must check that every media item recorded in the database has its file.
- **Revisit** if network images become a player-facing asset served on every page load, if more than a few hundred megabytes of images or templates build up, or if a second backend instance needs shared storage.

## Decisions needed

1. Keep network images and card templates in the database, as recommended, or move them to the media folder.
2. Whether a later storage change should also keep replaced files (as network images and templates do now) or delete them (as news media does when removed).
3. Whether to add the backup rule above to SPEC-09 and SPEC-18 now, ahead of their deployment rows in SPEC-22.

## Acceptance criteria

- The user has chosen a direction for each existing upload feature, and the choice is recorded here.
- Any approved migration has its own SPEC-22 row, specification, and tests, including moving existing data without losing history.
- Deployment and backup documentation names every place uploaded bytes are kept.
