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
