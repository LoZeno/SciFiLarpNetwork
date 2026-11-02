# SPEC-06: Avalonia management application

## Requirement

Replace the WinForms database-management tool with an Avalonia desktop application. It is an administrative client of the backend, not a direct database editor.

## Initial administration areas

Consider the legacy manager's player/character, skill/ability, event, message, news, object/item, substance, datapad/holodisk, infection, and event-printing workflows when defining the new administration scope.

## Acceptance criteria

- The desktop client is built with Avalonia and uses the same backend API and authorization rules as other clients.
- Consume the backend's documented Siren profile for resource links and available actions; do not hard-code discoverable API routes or treat Siren classes as desktop-widget instructions. Keep purpose-built management workflows in the Avalonia client.
- Resolve Siren links/actions only within the configured API origin, and rely on server-side authentication, authorization, validation, and domain rules for every operation.
- English is the default UI language; complete English and Italian translations and extensible localization resources are provided in accordance with SPEC-20.
- It provides responsive, accessible workflows for in-scope administrative tasks and surfaces validation and server errors clearly.
- It does not open or modify the event store directly.
- The application builds and launches on every supported OS/architecture combination in SPEC-07.
- The installed management client can connect to a configured local host and complete its in-scope workflows without internet access, external identity services, remote content, or runtime dependency downloads, as required by SPEC-21.
