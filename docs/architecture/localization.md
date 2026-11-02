# Localization conventions

English (`en`) is the source and fallback language. Italian (`it`) is the initial additional supported language. Locale resource files are UTF-8 JSON string maps in the repository-level `Localization/` directory, with stable, namespaced keys and matching key sets in every locale. Add a language by adding its locale JSON file and translating every existing key; do not change application or domain logic to add a locale.

Run the locale completeness check from the repository root:

```powershell
node Localization/validate.mjs
```

From `Frontend/`, run `npm run locales:check` or `node ../Localization/validate.mjs`. The frontend's `npm run check` also runs this validator. It verifies that English and Italian are present, JSON resources parse, every locale has exactly the English key set, and no translation value is empty. Add framework-specific missing-key/runtime fallback checks with the corresponding application integration.

Use namespaced keys such as `common.save`, `validation.required`, and `errors.unauthorized`. Backend error codes are stable machine-readable identifiers and map to localized resource keys; clients must not infer behavior by parsing English error prose. Keep locale selection separate from formatting and storage: use locale-aware formatters for dates, times, and numbers, while persisting culture-independent values.

English is the default when no valid user preference or deployment default is configured. Resolve deployment defaults and persisted user preferences at the application boundary; anonymous preference persistence is client-local. Applying a changed locale must not discard unsaved user work. The exact preference synchronization mechanism is established with the authenticated application workflows.

Only application-owned UI, accessibility, validation, and error text belongs in translation resources. Never automatically translate user-authored Player, Character, Event, Message, News story, Personal file, Item, or other LARP content. Preserve its Unicode text as entered, including Italian diacritics.

The shared resources establish the baseline strings and key contract before feature UI exists. Each application may add its own namespaced keys to the same locale files. The React client imports these shared JSON resources, uses English for an unset/invalid preference, and sends the selected locale through `Accept-Language`; runtime assets must remain local for offline venue operation. Backend and Avalonia integration follow their corresponding roadmap tasks.
