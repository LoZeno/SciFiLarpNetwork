# Web feature: About and version

**Legacy route:** `About.aspx`.

## Observed behavior

Displays assembly title, file version, and copyright metadata.

## Acceptance criteria

- The page displays the generic product name, release version, and approved copyright/licensing information from build metadata.
- Version information matches the deployed artifact and does not expose machine paths, build secrets, or sensitive environment details.
- The page is accessible and links to the repository's user/configuration documentation where appropriate.
