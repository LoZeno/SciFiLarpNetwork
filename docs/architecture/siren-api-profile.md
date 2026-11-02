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
- Unexpected server failures are logged with their exception and correlation context; responses do not disclose exception messages or stack traces.

## Compatibility

- The backend serves the Siren media type for successful Siren resources and Siren errors. OpenAPI remains served as `application/json`.
- Clients tolerate unknown optional properties, classes, relations, and action fields; they must not treat an unknown affordance as permission to execute it.
- Contract tests cover serialization names/shapes, required links, media types, request localization, model validation, machine-readable errors, authorization challenges, and concurrency conflicts.
