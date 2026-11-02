# SPEC-04: React and TypeScript web client

## Requirement

The web application is a React single-page application written in TypeScript. It consumes the backend's documented HTTP API and does not use server-rendered Web Forms, Razor views, or a second frontend runtime.

## Acceptance criteria

- Production UI code is TypeScript, with strict type checking enabled.
- English is the default UI language, with complete English and Italian translations and an extensible localization-resource structure as specified in SPEC-20.
- Client-side routing, API access, validation, and error/loading states are implemented using idiomatic React and TypeScript patterns.
- The client localizes user-facing text and formats dates, times, and numbers using the selected locale; API error codes are stable and mapped to localized messages.
- The SPA can be developed, tested, and built independently, and its production assets can be served in the documented deployment arrangement.
- The production SPA and every required runtime asset are served locally; no CDN, remote font/script/style, external API, or internet connection is required, as specified in SPEC-21.
- The client communicates with the backend over its versioned/documented API; persistence and domain rules remain on the server.
- Consume the backend's documented Siren profile (`application/vnd.siren+json`): follow advertised links/actions instead of hard-coding resource URLs, and construct navigation/sidebar entries from the documented Siren navigation relations/classes and localized titles.
- Treat Siren `class` values as resource/representation semantics, not generic widget identifiers. Support generic rendering for simple action forms where appropriate, but keep the application shell and purpose-built complex workflows as explicit React UI; do not assume a new backend resource can introduce arbitrary frontend controls or interaction behavior without client changes.
- Resolve Siren targets relative to the response/configured API base and reject targets outside the configured same-origin API boundary. Never execute an action automatically, and treat server-advertised actions as affordances rather than proof of authorization.
- Handle unknown resource classes, relations, and action fields safely and accessibly; present supported actions with localized labels and map stable server error codes to localized client messages.
- Core UI and workflows remain usable on a closed intranet with public internet access unavailable; optional outbound features cannot block startup or core tasks.

## Baseline implementation

- `Frontend` now contains a React/TypeScript SPA built with Vite, strict TypeScript, React Router, Vitest, and React Testing Library. `npm run dev`, `npm run check`, and `npm run build` support independent development, verification, and local production packaging.
- The application discovers the API root and follows its advertised latest-version link. It builds navigation from Siren navigation relations and resource targets, while treating classes as semantics rather than UI controls.
- The Siren client validates response structure, same-origin API-path targets, response media types, stable error codes, and supported JSON action requests. Advertised actions are user-submitted only; unsupported actions are exposed as unavailable.
- English is the default with optional locally persisted English/Italian preference. UI strings use shared repository locale resources; `Accept-Language` is sent to the backend without reloading the current resource or discarding unsaved form values. Date, date-time, and numeric formatting use the selected locale.
- UI, API client, localization, and offline-asset checks run through `npm run check`; coverage currently exceeds the SPEC-10 80% statement and line thresholds.
- The backend currently exposes only discovery roots, so the baseline shell has no feature navigation until later endpoint specifications. The production build is available under `Frontend/dist`; integrating it into the self-hosted application remains part of the deployment specification.
