# SPEC-06: Avalonia management application

## Requirement

Replace the WinForms database-management tool with an Avalonia desktop application. It is an administrative client of the backend, not a direct database editor.

## Initial administration areas

Consider the legacy manager's player/character, skill/ability, event, message, news, object/item, substance, datapad/holodisk, infection, and event-printing workflows when defining the new administration scope.

## Acceptance criteria

- The desktop client is built with Avalonia and uses the same backend API and authorization rules as other clients.
- It provides responsive, accessible workflows for in-scope administrative tasks and surfaces validation and server errors clearly.
- It does not open or modify the event store directly.
- The application builds and launches on every supported OS/architecture combination in SPEC-07.
