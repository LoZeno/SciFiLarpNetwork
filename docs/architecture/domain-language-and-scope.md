# Product language and initial capability scope

## Product identity

The working, franchise-neutral product description is **Sci-Fi LARP Network**. This is a descriptive working name, not a final brand decision. “Holonet” and other setting-specific names are legacy references only and must not appear as required product names, system concepts, seeded content, or UI labels.

The software is a configurable in-game information network and event-management platform for science-fiction live-action role-playing games. Each LARP may configure its own names, networks, groups, species, skills, labels, themes, and fictional content. Configuration must not alter authorization rules or require a particular fictional universe.

## Canonical language

| Preferred term | Meaning and usage |
|---|---|
| **LARP** | The live-action role-playing game using the platform. |
| **Player** | A real participant or account holder. Keep separate from their fictional roles. |
| **Character** | A fictional persona controlled by a player. A player may have multiple characters if the campaign permits. |
| **Account** | An authenticated application identity. It is not automatically the same as a character or an in-game network account. |
| **Network** | A configurable in-game information service or communication environment. Do not use a fixed franchise/faction as its definition. |
| **Group** | A configurable in-game organization or affiliation. Use “faction” only as setting-configured display terminology, not a universal domain primitive. |
| **Event** | A scheduled LARP gathering, with configurable dates/days and character participation. |
| **Skill** | A configurable character capability used by game rules. Avoid magic numeric skill IDs in user-facing terminology or feature logic. |
| **Message** | An in-game communication with sender, recipients, delivery/read state, and per-character mailbox entries. |
| **News story** | A network-scoped, time-bounded in-game publication. |
| **Personal file** | A character-owned saved in-game document. |
| **Datapad** | A configurable physical/digital prop that grants access to a list of in-game files. Renameable in setting UI. |
| **Item / component** | A configurable game object and its ingredients/parts, with optional QR identification and print data. |
| **Substance / ingredient** | A configurable fictional material, medicine, toxin, or other LARP-defined category. The category vocabulary is setting-owned. |
| **Condition** | A configurable fictional character state. “Infection” is one possible setting term, not a universal assumption or a real medical diagnosis. |
| **QR code / label** | A machine-readable identifier that resolves through an authorized application workflow. A code is not trusted merely because it decoded successfully. |
| **Simulated hacking** | A fictional, bounded game challenge that changes only in-game state. It is not real computer intrusion or a security control. |
| **Protection / difficulty** | Configurable game-rule values used to resolve fictional challenges. They are not real-world security guarantees. |

Use plain, inclusive English as the source language, and maintain English/Italian localization as required by SPEC-20. Keep user-authored story, message, character, and item content intact rather than auto-translating it.

## In-scope initial product capabilities

The following legacy-derived capabilities are approved for the initial product scope. Their acceptance criteria are defined in the linked feature specifications; this scope statement does not authorize implementation ahead of the SPEC-22 sequence.

| Capability area | Initial scope | Specification coverage |
|---|---|---|
| Players and characters | Administer player/account associations, character profiles, configured affiliations, skills, identity labels, and printable character cards. Keep real account authentication separate from character records. | [MANAGER-01](../specifications/legacy-features/manager-tabs/MANAGER-01-players-characters.md), [WEBFORM-38](../specifications/legacy-features/webforms/WEBFORM-38-character-home.md) |
| Skills and character abilities | Configure skill groups and skills used in game rules and authorized workflows. Translate legacy numeric skill identifiers into named configuration/domain concepts. | [MANAGER-02](../specifications/legacy-features/manager-tabs/MANAGER-02-skills.md) |
| Networks and sign-in | Configure generic networks, select an available network, authenticate accounts, apply character/network eligibility, and support account lifecycle and logout. Historical faction-specific login pages become configuration/aliases, not hard-coded factions. | [WEBFORM-39](../specifications/legacy-features/webforms/WEBFORM-39-network-selection.md), [WEBFORM-03](../specifications/legacy-features/webforms/WEBFORM-03-membership-login.md), [WEBFORM-27](../specifications/legacy-features/webforms/WEBFORM-27-faction-login-independent-planets.md), [WEBFORM-28](../specifications/legacy-features/webforms/WEBFORM-28-default-network-login.md), [WEBFORM-29](../specifications/legacy-features/webforms/WEBFORM-29-faction-login-empire.md), [WEBFORM-30](../specifications/legacy-features/webforms/WEBFORM-30-faction-login-republic.md), [WEBFORM-31](../specifications/legacy-features/webforms/WEBFORM-31-qr-login-prototype.md), [WEBFORM-01](../specifications/legacy-features/webforms/WEBFORM-01-change-password.md), [WEBFORM-02](../specifications/legacy-features/webforms/WEBFORM-02-password-changed.md), [WEBFORM-04](../specifications/legacy-features/webforms/WEBFORM-04-registration.md), [WEBFORM-26](../specifications/legacy-features/webforms/WEBFORM-26-end-session.md) |
| Events and participation | Create events and event days, register characters, manage participation/points while permitted, and produce participant printouts. | [MANAGER-03](../specifications/legacy-features/manager-tabs/MANAGER-03-events.md), [MANAGER-10](../specifications/legacy-features/manager-tabs/MANAGER-10-event-printing.md) |
| Messages and contacts | Character-owned contacts; inbox/outbox, compose, reply, read state, mailbox deletion, and personal-file copy. Historical duplicate routes converge on one implementation. | [MANAGER-04](../specifications/legacy-features/manager-tabs/MANAGER-04-messages.md), [WEBFORM-32](../specifications/legacy-features/webforms/WEBFORM-32-messages-legacy.md), [WEBFORM-33](../specifications/legacy-features/webforms/WEBFORM-33-messages.md), [WEBFORM-34](../specifications/legacy-features/webforms/WEBFORM-34-contacts.md), [WEBFORM-35](../specifications/legacy-features/webforms/WEBFORM-35-contacts-legacy.md) |
| News | Administer network-scoped, scheduled news and provide a player-facing feed. Simulated editing after an in-game challenge is covered under simulated hacking. | [MANAGER-05](../specifications/legacy-features/manager-tabs/MANAGER-05-news.md), [WEBFORM-36](../specifications/legacy-features/webforms/WEBFORM-36-news-feed.md), [WEBFORM-14](../specifications/legacy-features/webforms/WEBFORM-14-hacked-news.md) |
| Files and datapads | Character-owned personal files; configured datapads containing readable files; manual and QR access workflows. Fictional encryption is not a substitute for real security. | [MANAGER-08](../specifications/legacy-features/manager-tabs/MANAGER-08-datapads-files.md), [WEBFORM-08](../specifications/legacy-features/webforms/WEBFORM-08-personal-files.md), [WEBFORM-17](../specifications/legacy-features/webforms/WEBFORM-17-manual-datapad-reader.md), [WEBFORM-18](../specifications/legacy-features/webforms/WEBFORM-18-qr-datapad-reader.md) |
| Items and components | Configure, compose, identify, and print generic items and their components, including QR labels. | [MANAGER-06](../specifications/legacy-features/manager-tabs/MANAGER-06-items-components.md), [WEBFORM-19](../specifications/legacy-features/webforms/WEBFORM-19-identify-item.md), [WEBFORM-25](../specifications/legacy-features/webforms/WEBFORM-25-technical-workbench.md), [MANAGER-10](../specifications/legacy-features/manager-tabs/MANAGER-10-event-printing.md) |
| Substances and workbenches | Configure ingredients/substances and recipes; identify permitted categories; simulate medical/chemical/biochemical analysis and technical assembly as LARP mechanics. No real-world medical or chemistry advice is provided. | [MANAGER-07](../specifications/legacy-features/manager-tabs/MANAGER-07-substances-ingredients.md), [WEBFORM-06](../specifications/legacy-features/webforms/WEBFORM-06-manual-medical-analysis.md), [WEBFORM-07](../specifications/legacy-features/webforms/WEBFORM-07-qr-medical-analysis.md), [WEBFORM-20](../specifications/legacy-features/webforms/WEBFORM-20-identify-substance.md), [WEBFORM-21](../specifications/legacy-features/webforms/WEBFORM-21-identify-toxin.md), [WEBFORM-23](../specifications/legacy-features/webforms/WEBFORM-23-biochemistry-lab.md), [WEBFORM-24](../specifications/legacy-features/webforms/WEBFORM-24-chemistry-lab.md) |
| Character conditions | Configure fictional conditions and assign/remove them on characters with an audit trail. The initial vocabulary is not limited to infections. | [MANAGER-09](../specifications/legacy-features/manager-tabs/MANAGER-09-infections.md) |
| QR capture | Reusable offline-capable scanner and camera-independent input paths shared by login/identity, samples, items, substances, components, and datapads as individually authorized. | [CROSSCUT-01](../specifications/CROSSCUT-01-qr-capture.md) |
| Fictional network intrusion | Cinematic, purely in-game challenges for configured network/account/data scenarios. Successful results may grant only explicitly scoped in-game reads/edits/copies. | [CROSSCUT-02](../specifications/CROSSCUT-02-cinematic-hacking.md), [WEBFORM-09](../specifications/legacy-features/webforms/WEBFORM-09-hacked-account-menu.md), [WEBFORM-11](../specifications/legacy-features/webforms/WEBFORM-11-hacked-files.md), [WEBFORM-12](../specifications/legacy-features/webforms/WEBFORM-12-hacked-inbox.md), [WEBFORM-13](../specifications/legacy-features/webforms/WEBFORM-13-hack-network-account.md), [WEBFORM-15](../specifications/legacy-features/webforms/WEBFORM-15-hacked-outbox.md), [WEBFORM-16](../specifications/legacy-features/webforms/WEBFORM-16-network-account-list.md) |
| Event print operations | Generate printable character, advanced-skill, item, substance, ingredient, and datapad labels/cards from event assignments and catalog data. | [MANAGER-01](../specifications/legacy-features/manager-tabs/MANAGER-01-players-characters.md), [MANAGER-03](../specifications/legacy-features/manager-tabs/MANAGER-03-events.md), [MANAGER-06](../specifications/legacy-features/manager-tabs/MANAGER-06-items-components.md), [MANAGER-07](../specifications/legacy-features/manager-tabs/MANAGER-07-substances-ingredients.md), [MANAGER-08](../specifications/legacy-features/manager-tabs/MANAGER-08-datapads-files.md), [MANAGER-10](../specifications/legacy-features/manager-tabs/MANAGER-10-event-printing.md) |
| Product information | Display generic product name, version, and approved licensing/copyright information. | [WEBFORM-37](../specifications/legacy-features/webforms/WEBFORM-37-about.md) |

## Explicitly omitted or changed from legacy behavior

- **No legacy data import or backward compatibility is included.** The old destination implementation was removed under SPEC-12. Importing legacy SQLite databases requires a separately approved specification.
- **No franchise-specific product/network names, factions, species, story content, assets, colors, or terminology are required.** A LARP may provide its own configurable equivalents.
- **No separate contacts implementation or duplicate mailbox implementation.** The old contacts and mailbox routes converge on the canonical contacts and messaging capabilities; compatibility redirects are optional and must not create parallel behavior.
- **No standalone QR postback prototype.** QR scanning is one shared, local capability (CROSSCUT-01) used by business workflows; a separate scanner page is not part of the initial product.
- **No QR login based on a public character GUID.** QR sign-in is included only if implemented with the secure, revocable, replay-resistant identity-token requirements in WEBFORM-31 and CROSSCUT-01. Otherwise conventional configured authentication remains the sign-in path.
- **No compromised-contact feature in the initial scope.** The legacy page is a stub with no behavior. It remains hidden/omitted until a separate product decision establishes its game rules; the breached-account menu must not imply it works.
- **No real computer intrusion, shell/command execution, credential harvesting, network probing, or claims that fictional encryption protects data.** Hacking and encryption mechanics are in-game-only.
- **No real medical, infection diagnosis, toxicology, or chemical guidance.** Conditions, samples, substances, and laboratory outcomes are fictional, administrator-configured LARP content.
- **No assumption that every legacy page or manager control must be recreated as a separate screen.** Duplicate and incomplete routes may be consolidated or retired as specified, while preserving the approved underlying capability.
- **No direct database editing by the management client.** Administrative changes go through the backend and are recorded according to the event-sourcing specifications.
- **Banking, accounts/balances, doors, device-network control, and other data-only legacy entities without a corresponding approved feature specification are not part of the initial scope.** They can be proposed as later features.

## Scope acceptance criteria

- Every initial capability above maps to one or more feature specifications and is implemented only when reached in SPEC-22's order.
- UI and domain concepts use this glossary or explicitly configured setting-specific display labels; internal identifiers remain stable and franchise-neutral.
- A feature can be omitted from a particular LARP configuration without implying the feature is missing or broken.
- Any newly proposed capability, migration/import, or reversal of an explicit omission is separately specified and approved before implementation.
