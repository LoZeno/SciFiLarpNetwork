# Sci-Fi LARP Network Siren API profile

The backend uses Siren (`application/vnd.siren+json`) for successful API resource representations. Siren supplies resource metadata, relationships, navigational links, and available actions; it does not describe the React or Avalonia visual layout. OpenAPI describes endpoint schemas and complements this hypermedia contract.

## Entry points and identifiers

- The API entry point is `/api`. Clients discover the current API version through the `urn:scifilarnetwork:rels:latest-version` link. The initial versioned entry point is `/api/v1`.
- Every Siren resource representation has a `self` link. Version roots link to their parent with `up`; API roots link to the OpenAPI document with `service-desc`.
- Resource `class` values are lower-case, stable semantic identifiers such as `api`, `api-version`, and `error`. They identify the represented resource, not UI controls.
- Relation identifiers use registered Web Linking relation names where applicable. Product-specific relation identifiers use stable absolute URNs under `urn:scifilarnetwork:rels:`. Clients must not infer route meaning from an `href` or localized title.
- API-generated `href` values are relative to the configured same-origin API origin and include an application `PathBase` when configured. Do not construct these values from an untrusted request `Host` header.

## Navigation

- `home` denotes the current user's home resource when one exists.
- Sidebar/navigation links use the product relation `urn:scifilarnetwork:rels:navigation`. The server emits only links the current user may navigate to, in presentation order. The localized link `title` is the display label.
- Navigation affordances are contextual and may change with the current character, selected network, authorization, and game state. Clients render advertised navigation but must handle missing links and unknown relations gracefully.
- The initial unauthenticated API root has no application navigation links or domain actions; those appear with their feature/authentication endpoints.

## Collections

- Collections that can hide archived entries link to both views: `urn:scifilarnetwork:rels:with-archived` (archived entries included) and `urn:scifilarnetwork:rels:without-archived` (active entries only). Clients follow these links instead of adding query parameters.
- Catalog relations: `urn:scifilarnetwork:rels:talents`, `urn:scifilarnetwork:rels:skills`, and `urn:scifilarnetwork:rels:card-flags`. The talents navigation link carries the talents relation, and each talent item in a talent collection links to its own skills with the skills relation.
- Collections that can hide disabled entries link to `urn:scifilarnetwork:rels:with-disabled` and `urn:scifilarnetwork:rels:without-disabled` in the same way.
- Network relations: `urn:scifilarnetwork:rels:networks` marks the networks navigation link, and `urn:scifilarnetwork:rels:background-image` links a network (and its collection item) to its uploaded background image. A network without that link uses its theme's background image.
- Paged collections (players and characters, 50 entries per page in running-number order) link to their pages with the registered relations `first`, `prev`, `next`, and `last`; `prev` and `next` appear only when such a page exists. Page links keep the collection's search text and archived filter. Clients follow these links instead of building page addresses.
- Player relations: `urn:scifilarnetwork:rels:players` marks the players navigation link, `urn:scifilarnetwork:rels:characters` links to the character search collection, `urn:scifilarnetwork:rels:character-stats` to the character stat catalog, and `urn:scifilarnetwork:rels:player` links a character to its player. Player and character resources also link to the network catalog with the networks relation.
- Character skill relations: `urn:scifilarnetwork:rels:character-skills` links a character to its talents and skills. That resource links back with `urn:scifilarnetwork:rels:character`, and to the talent and skill catalogs with the talents and skills relations. Administrators get its `save-character-skills` action, a `PUT` of the character `version`, every `talentIds` entry, and every owned skill as `skills` entries of `skillId`, `talentId`, `count`, and `specifics`; both lists replace the stored ones.
- Searching uses `GET` actions such as `search-players`, `search-characters`, and `find-character-by-qr`. As Siren defines for `GET` actions, the client sends the action's declared fields as query parameters on the action's `href`, and sends no other parameters.

## Binary resources

- Images are not Siren resources. A link with a binary relation (such as `background-image`) points to a `GET` endpoint that returns the image with its own media type (`image/png`, `image/jpeg`, or `image/webp`), `X-Content-Type-Options: nosniff`, and long private caching, because an image identifier always names the same bytes. The same authentication and authorization apply as for Siren resources.
- Uploads are JSON actions: the file travels base64-encoded in a `content` field, and the server detects the format from the bytes rather than trusting a client-supplied name or type.
- An action can answer with a file instead of a Siren entity. The player collection's `print-character-cards` action (`POST`, `application/json`, field `characterIds`) returns `application/pdf` on success; it is advertised to readers and administrators because printing changes nothing. The client accepts both `application/pdf` and the Siren media type, because errors are still Siren error entities.

## Actions

- Action names and resource classes are stable, language-neutral identifiers. `title` and field `title` are localized human-readable strings.
- Use explicit action `name`, `href`, `method`, and `type`. For JSON submissions, `type` is `application/json`; `fields[].name` maps to a property of the typed JSON request object. The client submits only the values it supports and does not infer a body schema from labels.
- Simple action fields use supported Siren input types documented by the endpoint. Rich controls, nested editing, QR workflows, and other specialized experiences remain purpose-built client behavior.
- Advertised actions are affordances, not credentials or authorization grants. Each request independently authenticates the caller, authorizes current state, validates input and invariants, and applies concurrency checks. Unauthorized operations return 401/403; stale writes return 409 with a stable error code.
- Clients never execute actions automatically. They resolve relative targets against the configured API origin and reject absolute or cross-origin targets.

## Localization and errors

- `Accept-Language` selects among `en` and `it`; invalid or unsupported values fall back to English. Human-readable Siren titles and field labels follow that culture.
- Classes, relation identifiers, action names, property names, and error `code` values are stable and never localized.
- Errors use a Siren entity with `class: ["error"]`, `self` link, and properties `code`, `title`, `detail`, `traceId`, and `status`. Validation errors may include an `errors` object keyed by request field. Clients branch on `code`, not prose.
- The initial error codes are `errors.validation`, `errors.unauthorized`, `errors.forbidden`, `errors.notFound`, `errors.conflict`, `errors.requestFailed`, `errors.serviceUnavailable`, and `errors.internalServerError`.
- The talent and skill catalog adds `errors.staleVersion`, `errors.nameAlreadyUsed`, `errors.invalidName`, `errors.invalidCost`, `errors.unknownTalent`, `errors.talentArchived`, `errors.unknownCardFlag`, `errors.invalidCardFlagName`, `errors.emptyPrerequisiteGroup`, `errors.selfPrerequisite`, `errors.duplicatePrerequisite`, `errors.unknownPrerequisite`, and `errors.prerequisiteLoop`. Each code is also its localization key.
- A skill change returns 409 `errors.concurrentChange` when another command changed shared data first (the prerequisite graph or an assigned talent) but not the skill itself. Nothing was saved; clients keep the user's input and let them save again. 409 `errors.staleVersion` still means the edited item itself changed.
- The network catalog adds `errors.codeAlreadyUsed`, `errors.invalidCode`, `errors.invalidTheme`, `errors.invalidImage`, `errors.imageTooLarge`, `errors.imageDimensionsTooLarge`, `errors.networkDisabled`, and `errors.defaultNetworkCannotBeDisabled`, and reuses `errors.staleVersion`, `errors.nameAlreadyUsed`, and `errors.invalidName`.
- Players, characters, and character stats add `errors.invalidEmail`, `errors.invalidPhone`, `errors.invalidNotes`, `errors.invalidPlayerType`, `errors.invalidCharacterText`, `errors.invalidExperiencePoints`, `errors.invalidCharacterStatus`, `errors.deathDateRequired`, `errors.playerInactive`, `errors.playerNotInactive`, `errors.characterNotArchivable`, `errors.archived`, `errors.unknownNetwork`, `errors.defaultNetworkNotSelectable`, `errors.disabledNetworkNotSelectable`, `errors.unknownStat`, `errors.invalidStatValue`, `errors.invalidStatName`, `errors.invalidStatType`, `errors.invalidQrIdentifier`, and `errors.statArchived`, and reuse `errors.staleVersion`, `errors.concurrentChange`, `errors.nameAlreadyUsed`, and `errors.invalidName`. 409 `errors.concurrentChange` means another command changed shared data first (a running-number sequence, or the player while one of its characters was created or made Active); nothing was saved and saving again re-checks every rule. Player and character creations retry up to three times on their own before returning it. Creating a character or making one Active raises its player's `version`, so a player edit based on the earlier version returns 409 `errors.staleVersion`.
- Character talents and skills add `errors.tooManyTalents`, `errors.duplicateTalent`, `errors.duplicateSkillPurchase`, `errors.invalidPurchaseCount`, `errors.invalidSpecifics`, `errors.unknownSkill`, and `errors.talentNotOwned` (400), and `errors.archivedTalentNotSelectable`, `errors.skillNotInTalent`, `errors.skillArchived`, `errors.notMultiPurchase`, `errors.prerequisitesNotMet`, `errors.requiredByOtherSkills`, and `errors.insufficientFreePoints` (409), and reuse `errors.unknownTalent` and `errors.staleVersion`. An error about particular skills lists their names in the error's `errors.skills`; for `errors.requiredByOtherSkills` these are the owned skills that still need a removed skill. Saving talents and skills raises the character's `version`, and a character edit that lowers experience points below the points spent on skills returns 409 `errors.insufficientFreePoints`.
- Character cards add `errors.noCharactersSelected`, `errors.tooManyCharacters` (more than 200 cards), and `errors.unknownCharacters` (400; the unknown identifiers are listed in the error's `errors.characterIds`), and `errors.cardRenderingFailed` (500, when Typst fails or cannot start; the Typst error is logged on the server).
- Administrator account creation and role changes return 409 `errors.staleVersion` when another administrator command committed first. Nothing was saved; a retry re-checks every rule, and an actor who has really lost the administrator role then gets 403 `errors.forbidden`.
- Unexpected server failures are logged with their exception and correlation context; responses do not disclose exception messages or stack traces.

## Compatibility

- The backend serves the Siren media type for successful Siren resources and Siren errors. OpenAPI remains served as `application/json`.
- Clients tolerate unknown optional properties, classes, relations, and action fields; they must not treat an unknown affordance as permission to execute it.
- Contract tests cover serialization names/shapes, required links, media types, request localization, model validation, machine-readable errors, authorization challenges, and concurrency conflicts.
