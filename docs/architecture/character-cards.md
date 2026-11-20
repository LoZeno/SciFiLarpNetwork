# Character cards

Character cards ([MANAGER-13](../specifications/legacy-features/manager-tabs/MANAGER-13-character-cards.md)) are PDFs made by the backend with [Typst](https://typst.app). This guide describes how they are made, the data a card template receives, and how Typst is bundled. User-supplied templates are added by [SPEC-29](../specifications/SPEC-29-card-templates.md); until then the backend uses its default template.

## How a PDF is made

1. The player collection advertises `print-character-cards` (see the [Siren API profile](siren-api-profile.md)). The request lists character identifiers in print order.
2. The backend collects each character's current data and applies the card rules (`CharacterCardComposer`). Characters are loaded one after another (character, player, and skills each), so 200 cards take up to 600 queries; batch loading is the fix if this gets slow.
3. The renderer creates a new temporary folder holding the template (`character-cards.typ`) and the data (`cards.json`), and runs:

   ```text
   typst compile --root <folder> --ignore-system-fonts character-cards.typ cards.pdf
   ```

   The folder is the Typst root, so a template can read only these two files. System fonts are ignored, so the output is the same on every host; the fonts built into Typst (for example Libertinus Serif and New Computer Modern) are available. Environment variables starting with `TYPST_` are not passed to Typst. If Typst runs longer than `CardPrinting:Timeout` (one minute by default), it is stopped and waited for. The folder is deleted afterwards; if a file is still held, the folder is left and a warning is logged, so cleanup never changes the result.
4. The QR image is made by the backend (QRCoder) as SVG and passed in the data, so the default template needs no Typst packages and rendering never uses the network.

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
- To update Typst, change `TypstVersion` and every `TypstSha256` together, using the digests published on the release.
