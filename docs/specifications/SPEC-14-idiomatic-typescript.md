# SPEC-14: Idiomatic TypeScript

## Requirement

Write the React client in idiomatic, current TypeScript, using language and React features where they improve clarity, type safety, and maintainability.

## Acceptance criteria

- Strict TypeScript checks are enabled; avoid `any` and unsafe type assertions except for documented, narrowly justified boundaries.
- Components and hooks have clear responsibilities, and API/domain data is represented by explicit types.
- Formatting, linting, type checking, and frontend tests are automated and consistently applied.
- Client state and error handling are explicit; avoid duplicating authoritative server-side domain rules in the UI.
