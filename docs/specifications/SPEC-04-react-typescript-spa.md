# SPEC-04: React and TypeScript web client

## Requirement

The web application is a React single-page application written in TypeScript. It consumes the backend's documented HTTP API and does not use server-rendered Web Forms, Razor views, or a second frontend runtime.

## Acceptance criteria

- Production UI code is TypeScript, with strict type checking enabled.
- Client-side routing, API access, validation, and error/loading states are implemented using idiomatic React and TypeScript patterns.
- The SPA can be developed, tested, and built independently, and its production assets can be served in the documented deployment arrangement.
- The client communicates with the backend over its versioned/documented API; persistence and domain rules remain on the server.
