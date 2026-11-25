# SPEC-31: Media folder layout and network images on disk

## Purpose

Carry out the storage decisions recorded in [SPEC-30](SPEC-30-uploaded-file-storage-review.md) (2026-11-25): pictures and videos live in the media folder (`Media:RootPath`), one subfolder per feature, and network background images move there from the database.

## Scope

| Feature | Before | After |
|---|---|---|
| News media ([MANAGER-05](legacy-features/manager-tabs/MANAGER-05-news.md)) | `Media:RootPath/{mediaId}` | `Media:RootPath/News/{mediaId}` |
| Network background images ([MANAGER-11](legacy-features/manager-tabs/MANAGER-11-networks.md)) | `NetworkImageDocument` in the database | `Media:RootPath/NetworkImages/{imageId}` |
| Card templates ([SPEC-29](SPEC-29-card-templates.md)) | `CardTemplateContentDocument` in the database | Unchanged |

Out of scope: the shared, cross-feature media library. It is built with [MANAGER-08](legacy-features/manager-tabs/MANAGER-08-datapads-files.md), the first feature that needs to reuse pictures from another feature (see SPEC-30).

## Requirements

### One media file store

- One backend media file store writes, reads, and deletes files for every feature. Each feature names its own subfolder; the subfolder name is fixed in code, never taken from a request.
- Files are named by their identifier only. A user-supplied name never reaches a path.
- The write path is the one news already uses: write a temporary file, check it, give it its final name, then record the event; delete the file again when the event does not commit.

### News media

- New and existing news files are read and written under `News/`. Behaviour, limits, events, routes, and range requests are unchanged.

### Network background images

- An upload writes the file to `NetworkImages/{imageId}`. Each upload gets a new identifier, so a file is never overwritten.
- Replaced and removed images are kept. History keeps its references; nothing is deleted or renamed. The current image is the one named by the network's latest image event; the upload time is in that event.
- Events, routes, limits, format checks, and the Management app are unchanged. The image route serves the stored file with its recorded type and `X-Content-Type-Options: nosniff`.
- `NetworkImageDocument` and its bytes in the database are removed. A byte-less `NetworkImageFileDocument` (image identifier, network, type, size) is saved in the same session as the image event, so the image route finds the recorded type of any image, current or replaced (decided by the user on 2026-11-25).

### Existing data

- No installation has been deployed, so there is no production data to move. Development databases and media folders are reset. If that is no longer true when this row starts, stop and add a data move (copy each `NetworkImageDocument` to its file and each root news file to `News/`, without changing events) before removing the old storage.

### Documentation

- Update MANAGER-05, MANAGER-11, and [backend API](../architecture/backend-api.md) to name the new folders.
- Deployment and backup documentation keeps naming every place uploaded bytes are kept: the database (events, card templates) and `Media:RootPath` (news media, network images).

## Acceptance criteria

- News media and network images are stored in their subfolders, through one media file store.
- Uploading a second network image keeps the first file; the network shows the new one; history still resolves the old identifier to its file.
- A failed event append leaves no committed file behind.
- No path is built from request data other than a parsed identifier.
- Existing news, network, and card-template tests pass unchanged except where they assert the storage location.

## Verification

- Integration tests: news upload, download with range, and removal under `News/`; network image upload, replacement keeping the old file, download of current and old images, and removal keeping the file; a failed append leaves no committed file.
- Tests prove each new test fails when the behaviour is switched off.
