# SPEC-05: ASP.NET Core MVC backend

## Requirement

Implement the backend as an ASP.NET Core MVC application targeting .NET 10. MVC controllers expose HTTP endpoints for the SPA and administration client. Domain behavior and persistence concerns must remain separated from transport and presentation concerns.

## Hypermedia API format

Use [Siren](https://github.com/kevinswiber/siren) (`application/vnd.siren+json`) as the API's hypermedia representation format. The SciFiLarpNetwork profile is documented in [Siren API profile](../architecture/siren-api-profile.md): API entry point, resource-class vocabulary, link-relation vocabulary, navigation conventions, action/input conventions, localization behavior, and error representation. Keep OpenAPI documentation for endpoint schemas and client tooling; Siren complements rather than replaces it.

- Return Siren representations for successful resource/workflow responses, with `self` links and only the relevant navigational links and currently available actions.
- Use Siren `class` values to identify resource or representation semantics, not React/Avalonia widget names. Siren describes resources and affordances; it is not a recursive UI-layout or component-rendering protocol.
- Define a documented navigation convention (including `home` and sidebar/navigation links) using link relations and, where needed, Siren link `class`. Preserve the response order for navigation items. Use stable relation identifiers rather than having clients infer routes from display labels or URLs.
- Represent simple submissions as Siren actions with an explicit name, title, HTTP method, target, request media type, and fields. Document supported field types and the mapping to request payloads. Do not treat action fields as a complete description of arbitrary screens or complex workflows.
- Localize human-readable Siren titles and field labels according to the negotiated/requested application language. Keep relation identifiers, action names, resource classes, and machine-readable error codes stable and language-neutral.
- Return stable machine-readable error codes and a documented error representation for validation, authorization, concurrency, and unexpected failures. Siren affordances are advisory: every action request must independently authenticate, authorize, validate state, and enforce domain invariants.
- Treat Siren `href` values as untrusted navigation input on clients. Generate same-origin API targets, honor configured application base paths, and never rely on a client-visible link or action as an authorization boundary.

## Acceptance criteria

- The backend uses ASP.NET Core MVC controllers and standard dependency injection/configuration/hosting facilities.
- HTTP endpoints use typed request/response contracts, appropriate status codes, input validation, and consistent error responses.
- The API implements and documents the Siren profile above, including relation/class/action vocabularies, media-type behavior, localization, and machine-readable errors; endpoint schemas remain available through OpenAPI.
- Contract tests verify Siren serialization and required links, state-appropriate actions, action field/request mapping, stable error codes, authorization rejection, and the 409 conflict response used for stale writes. Feature-level tests must additionally verify real concurrency checks before those actions are delivered.
- Backend user-facing validation/errors support English and Italian; API errors include stable machine-readable codes and do not require clients to parse localized prose, as specified in SPEC-20.
- API documentation is available to developers and can be used to verify client/server contract compatibility.
- Environment-specific settings and secrets are supplied through configuration or secure deployment mechanisms, not machine-specific source-controlled paths.
- The backend, authentication/session handling, and API work on the local network without mandatory third-party identity, SaaS, DNS, or external API services; runtime outbound connectivity is not required (SPEC-21).
- Runtime dependencies and any required runtime are supplied or installed from local packages; core startup and data access do not depend on downloading packages or contacting public services.

## Baseline implementation

- `Backend/src/SciFiLarpNetwork.Backend.csproj` is an SDK-style .NET 10 MVC application in the root solution. It provides `/api`, `/api/v1`, and `/openapi/v1.json`.
- Siren entities, links, actions, fields, typed error properties, API exception handling, validation mapping, and authorization challenge/forbid responses are implemented under `Backend/src/Api`.
- Shared English/Italian resources are embedded from `Localization/`; `Accept-Language` negotiation defaults to English.
- ASP.NET Core configuration controls bind URLs and optional `PathBase`; local setup is documented in [backend API baseline](../architecture/backend-api.md). The API discovery baseline has no secrets, external identity provider, database access, or runtime network dependency.
- xUnit integration and serialization tests cover Siren/OpenAPI discovery, localized responses, typed action field/request mapping, validation, 401/403/404/409/500 errors, and exception detail redaction. Tests use an isolated test-only controller and authentication scheme, not production/game endpoints.
- The machine-readable OpenAPI document complements the [Siren API profile](../architecture/siren-api-profile.md). Feature-specific domain endpoints, sign-in/session issuance, database persistence, and feature actions remain for their later roadmap tasks.
