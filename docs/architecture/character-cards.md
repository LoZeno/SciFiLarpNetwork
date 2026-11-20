# Character cards

Character cards ([MANAGER-13](../specifications/legacy-features/manager-tabs/MANAGER-13-character-cards.md)) are PDFs made by the backend with [Typst](https://typst.app). This guide describes how they are made, the data a card template receives, uploaded templates, and how Typst is bundled.

## How a PDF is made

1. The player collection advertises `print-character-cards` (see the [Siren API profile](siren-api-profile.md)). The request lists character identifiers in print order.
2. The backend collects each character's current data and applies the card rules (`CharacterCardComposer`). Characters are loaded one after another (character, player, and skills each), so 200 cards take up to 600 queries; batch loading is the fix if this gets slow.
3. The renderer uses the template in use: the one an administrator uploaded ([SPEC-29](../specifications/SPEC-29-card-templates.md)), or else the default template embedded in the backend.
4. The renderer creates a new temporary work folder with an empty `packages` and `package-cache` folder and a `data` folder holding only the data (`cards.json`), and runs, from `data`:

   ```text
   typst --cert <work>/no-certificate.pem compile --root <work>/data --package-path <work>/packages --package-cache-path <work>/package-cache --ignore-system-fonts - <work>/cards.pdf
   ```

   The template is written to Typst's standard input (`-`), so it is not a file Typst can read. The `data` folder is the Typst root, so a template can read only `cards.json` (as `json("cards.json")` or `json("/cards.json")`); any other path, `..`, an absolute path, or a drive or network path fails to compile. System fonts are ignored, so the output is the same on every host; the fonts built into Typst (for example Libertinus Serif and New Computer Modern) are available. Environment variables starting with `TYPST_` and `NO_PROXY` are not passed to Typst. If Typst runs longer than `CardPrinting:Timeout` (one minute by default), it is stopped and waited for (at most 10 seconds, so a request never hangs). The folder is deleted afterwards; if a file is still held, the folder is left and a warning is logged, so cleanup never changes the result.
5. The QR image is made by the backend (QRCoder) as SVG and passed in the data, so templates need no Typst packages.

### Rendering never uses the network

Templates cannot use Typst packages. Three blocks stop them, each on its own:

| Block | Source | Effect |
|---|---|---|
| Empty `--package-path` and `--package-cache-path` | Documented: `typst compile --help` (local packages and package cache paths). | No package is found on the computer, so `@local` and any other namespace fail with "package not found". |
| `--cert` names a file that does not exist | The option is documented (`typst --help`: CA certificate for network requests). That it stops a download before any connection is **not documented — inferred** from testing Typst 0.15.1: `@preview` fails with "package not found" and Typst never prints "downloading". | `@preview` packages are never downloaded. |
| `HTTPS_PROXY`, `HTTP_PROXY`, `ALL_PROXY` (both cases) set to `http://127.0.0.1:9` | **Not documented — inferred** from testing: Typst 0.15.1 then reports "Connection Failed … actively refused" for `@preview`. | A download that did start could not leave the computer. |

`TemplateSandboxTests` runs these cases through the bundled Typst, and `TypstSandboxSetupTests` checks each block separately with a recording fake Typst.

## Uploaded templates

[SPEC-29](../specifications/SPEC-29-card-templates.md) lets administrators replace the default template. One template per card kind is kept on the `card-templates` stream of that kind (`CardTemplateUploaded`, `CardTemplateReset`); the uploaded source is stored as its own document, never in the event, and old uploads are kept. The uploader is the event's user name metadata, and the time is in the event.

- An upload is at most 256 KB. It is checked by printing four made-up sample cards (`SampleCharacterCards`: an archived character, one with an advanced skill, one with very long text, and one with a talent but no skills) with the labels of the request language; it is saved only if Typst makes the PDF. Typst's error is returned with the server's folder names removed.
- **Preview** prints the same sample cards with a file and saves nothing.
- **Reset** returns to the default template; resetting the default changes nothing.
- Upload and reset carry the template version they are based on; a stale version is rejected (`errors.staleVersion`). A database failure while saving gives `errors.cardTemplateNotSaved`.
- Download the template in use to start a new one; the default template below is the reference.

The default template lays out an A4 page as a 4 × 2 grid of 105 × 74.25 mm cards: each row is one card, front on the left and back on the right, with thin cut lines.

## Template data (`cards.json`)

Property names are camelCase. Values that are not set are `null`.

| Property | Type | Meaning |
|---|---|---|
| `language` | text | Two-letter language of the labels: `en` or `it`. |
| `labels` | object | Localized labels: `documentTitle`, `player`, `character`, `gender`, `species`, `type`, `experiencePoints`, `freePoints`, `skills`, `archived`. |
| `cards` | list | One entry per printed card, in print order. |

Each card:

| Property | Type | Meaning |
|---|---|---|
| `playerNumber`, `playerName` | number, text | The player's running number and display name. |
| `characterNumber`, `characterName` | number, text | The character's running number and name. |
| `title`, `gender`, `species`, `type` | text or `null` | Optional character details. |
| `qrIdentifier` | text | The character's public QR identifier. |
| `qrSvg` | text | The QR identifier drawn as SVG; use `image(bytes(card.qrSvg), format: "svg")`. |
| `experiencePoints`, `freePoints` | number | Experience points, and free points (negative when a skill's cost was raised). |
| `isArchived` | true/false | Whether the character is archived. |
| `cardFlags` | list of `{name, value}` | Every card flag, in name order, with its value (0 when no owned skill has it). |
| `stats` | list of `{name, value}` | Stats that are not archived, in name order; `value` is text. Empty Text values are left out. |
| `talents` | list of `{name, skills}` | The character's talents in picked order; `skills` is a list of `{name, count, isAdvanced}` in name order. A talent may have no skills. |

Skill descriptions, specifics notes, and credentials are never in the data.

## Bundled Typst

- `Backend/src/Host/Typst.targets` pins the Typst version and the SHA-256 of each release archive for the six supported runtimes. The runtime is the build's `RuntimeIdentifier`, or else the SDK's portable runtime (`NETCoreSdkPortableRuntimeIdentifier`, so distro-built SDKs still map to `linux-x64` or `linux-arm64`). On the first build for a runtime it downloads the archive from the official GitHub release (retrying up to three times), checks the hash, extracts it under `obj/typst/`, and copies `typst` (or `typst.exe`), `LICENSE`, and `NOTICE` to `tools/typst/` in the build and publish output. A wrong hash stops the build.
- The runtime is the one being published (`-r`), or else the computer doing the build. Building needs internet access once per runtime and version; the deployed backend never downloads anything.
- `CardPrinting:TypstPath` sets another Typst executable. `CardPrinting:Timeout` (default `00:01:00`) stops a render that takes too long.
- To update Typst, change `TypstVersion` and every `TypstSha256` together, using the digests published on the release. Then run `TemplateSandboxTests`: two of the package blocks rely on tested, not documented, Typst behavior.
