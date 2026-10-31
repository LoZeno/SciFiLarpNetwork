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
- Core UI and workflows remain usable on a closed intranet with public internet access unavailable; optional outbound features cannot block startup or core tasks.
