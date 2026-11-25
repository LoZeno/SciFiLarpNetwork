# Manager capability: Configurable networks

This capability is a new product prerequisite derived from the network-scoped workflows in WEBFORM-39 and the network-dependent manager and player features. It is not a request to reproduce franchise-specific factions.

## Purpose

Allow authorized administrators to define the generic in-game networks that characters may belong to and that player-facing workflows may select. The network catalog supplies stable references for Players and Characters, news, authentication, and later simulated challenges.

## Observed legacy behavior

- The legacy manager had no network tab. Networks were rows in a `Rete` table with a numeric identifier, a name (up to 60 characters), a description, and a faction label (up to 30 characters).
- A `RegistroIP` table mapped workstation IPv4 addresses to networks; the network selection page (WEBFORM-39) showed the networks registered for the caller's address.
- A per-network `Parametri` key/value table existed but was never used.
- Each network's look came from an ASP.NET theme hard-coded in its faction-specific login page.

## Product model

These rules replace the numeric identifiers, the faction label, the unused parameter table, and the hard-coded themes.

### Networks

- A network has a generated identifier, a code, a name, an optional description, a theme, an optional background image, an enabled flag, and a default flag. There is no faction field; groups are a separate concept.
- Names are required, are 1–64 characters after trimming, and are unique among all networks (case- and Unicode-normalization–insensitive).
- Codes are required, are 1–32 characters of lower-case letters `a`–`z`, digits, and hyphens, do not start or end with a hyphen, and are unique among all networks. Input is trimmed and lower-cased. A code is fixed when the network is created, so later features (for example sign-in addresses) can refer to it safely. Name, description, theme, and background image can be edited.
- Descriptions are optional and up to 2,000 characters.
- Networks are user content and are not translated.

### Themes

- Every network uses one of these product-defined themes: **Blue**, **Red**, **Green**, **Yellow**, **Amber**, **Grey**, **Purple**. Theme names are localized. There are no free colors.
- A theme is presentation metadata only. It never grants access or changes authorization.
- Each theme supplies its own background image. The player-facing theme assets are delivered with the first player screen, [WEBFORM-39](../webforms/WEBFORM-39-network-selection.md).

### Background images

- An administrator can upload a background image for a network. When present, it replaces the theme's background image, so several networks sharing a theme can still look different. Removing it restores the theme's background image.
- Accepted formats are PNG, JPEG, and WebP, up to 2 MB and 4096 × 4096 pixels. The server reads the format and the pixel size from the file's own header and rejects anything else, including SVG, an unreadable header, or a declared size above the limit, so a small file cannot claim a huge picture. Only the header is checked, so clients treat an image that fails to decode as no image, and an image that cannot be downloaded never blocks editing the network.
- Image files are kept in the `NetworkImages/` subfolder of the media folder set by `Media:RootPath`, named by their identifier ([SPEC-31](../../SPEC-31-media-folder-layout.md)). Each upload gets a new identifier, so a file is never overwritten. The network's events record only the image identifier, media type, and size. Replaced or removed images are kept, so history keeps its references; nothing is deleted.
- The management client shows a preview of the uploaded image.

### Enabling and disabling

- Networks can be enabled and disabled. Nothing is deleted.
- A disabled network keeps every existing character, event, news, and other reference. It is not offered for new selection or assignment; the consuming features enforce this when they assign or select a network.

### Default network

- At most one network is the default. The default must be enabled.
- An administrator sets the default with an explicit **Make default** action, which moves the default from the previous network in the same command.
- The default network cannot be disabled; choose another default first.
- When no default exists, the management client shows a warning, and consuming features that need a default (for example [WEBFORM-28](../webforms/WEBFORM-28-default-network-login.md)) require an intentional choice rather than guessing.

### Availability

- Workstation or address-based availability is not part of this capability. It is specified with network selection in [WEBFORM-39](../webforms/WEBFORM-39-network-selection.md), which must use explicitly configured trusted proxy and network rules and never untrusted client-supplied address headers.
- Network assignment and availability are revalidated server-side by every consuming command and query; a client-provided network identifier is never proof of membership or permission.

## Acceptance criteria

- Readers and administrators can list and search networks and see each network's code, name, theme, status, and default mark. The list can show or hide disabled networks. Only administrators can create, edit, upload or remove images, enable, disable, or change the default.
- Administrators can create networks and edit their name, description, theme, and background image.
- Validation enforces the rules above: required, unique names and codes; the code format; a known theme; the image formats and size; at most one default, which must be enabled; and no disabling of the default.
- Disabling a network keeps its history and references and does not move characters. Disabled networks are hidden from new selection by default and clearly marked when shown.
- Every change is an authorized, validated, auditable event-sourced command with optimistic concurrency. A stale edit is rejected with clear conflict feedback.
- The backend API, the management client, and the English/Italian resources handle empty catalogs, a missing default, disabled networks, invalid input and images, stale edits, and persistence failures clearly.
- The network catalog can be configured on an isolated local deployment with no external identity or network service.

## Verification

- xUnit tests cover name and code rules and uniqueness, theme validation, image format and size checks, the default-network invariants, disabling with existing references, authorization, event emission, and concurrency conflicts.
- API tests verify discovery links and actions for readers and administrators, validation and conflict errors, image upload, download and removal, and that readers cannot change anything.
- Management-client tests cover listing, searching and filtering, the create/edit dialog, image upload and preview, enable/disable and default changes, and error display.
