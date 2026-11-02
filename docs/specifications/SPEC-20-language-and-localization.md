# SPEC-20: Application language and localization

## Requirement

English is the default language and source language for all applications: the React web client, ASP.NET Core backend user-facing responses, and Avalonia management application. All applications must support localization in at least English and Italian, with an architecture that allows additional languages to be added without redesigning features.

## Acceptance criteria

- English is the default locale when no user preference or deployment default is configured.
- The React SPA and Avalonia application provide complete English and Italian translations for user-facing navigation, controls, labels, validation messages, empty/loading/error states, and accessibility text.
- User-facing backend errors and validation messages are localizable. API contracts use stable machine-readable error codes so clients can localize responses consistently; they do not depend on parsing English prose.
- Users can select a supported language, and their preference is applied consistently across the web and desktop clients where identity/settings are shared. Anonymous users receive a configurable default and can change it.
- Locale selection and formatting are distinct: dates, times, numbers, and currencies use the selected locale while stored values remain culture-independent and unambiguous.
- Translatable UI strings are externalized from application logic. Adding a language requires adding translation resources and validation, not changing feature/domain code.
- Missing translations are detected in development/CI and fall back to English without displaying raw keys or blank controls.
- Character, player, event, news, and other user-authored content is not automatically translated; it is displayed as entered, with Unicode and Italian diacritics preserved.
- Language preference persists across refreshes and application restarts as appropriate, and can be changed without losing unsaved work.
- Tests cover locale selection, English fallback, Italian text rendering, localized validation/error messages, and locale-sensitive formatting.
- README documents supported languages, default-language behavior, and how to configure/change the language.

## Baseline implementation

- `Localization/en.json` and `Localization/it.json` establish the shared UTF-8 JSON resource format and initial common, accessibility, validation, and error strings.
- `Localization/validate.mjs` checks that English and Italian resources exist, all locale key sets match English, keys are namespaced, and translation values are non-empty. `Frontend/npm run check` includes this validation.
- [Localization conventions](../architecture/localization.md) defines the English fallback, stable namespaced keys, backend error-code mapping, locale/formatting separation, preference boundaries, and user-authored content handling.
- Framework-specific resource loading, language selection UI, preference persistence/synchronization, runtime fallback, and feature-specific translations are implemented and tested with the relevant application roadmap tasks; this foundation does not claim those application behaviors are already delivered.
