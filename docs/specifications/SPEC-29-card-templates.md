# SPEC-29: Card templates

## Purpose and scope

Let administrators replace the look of printed cards with their own [Typst](https://typst.app) template. [MANAGER-13](legacy-features/manager-tabs/MANAGER-13-character-cards.md) ships the default character-card template and the card data; this specification adds uploading, checking, previewing, and resetting a template.

There is one template per card kind. This specification covers the **character card** kind. Later card kinds (items, substances, ingredients, datapads; [MANAGER-06](legacy-features/manager-tabs/MANAGER-06-items-components.md), [MANAGER-07](legacy-features/manager-tabs/MANAGER-07-substances-ingredients.md), [MANAGER-08](legacy-features/manager-tabs/MANAGER-08-datapads-files.md), [MANAGER-10](legacy-features/manager-tabs/MANAGER-10-event-printing.md)) each get one template of their own, following the same rules.

## Product model

- A template is one Typst source file (`.typ`). It reads the card data and the localized labels from the JSON file that MANAGER-13 defines. The JSON structure is documented for template authors, with the default template as the example.
- A template is stored by the backend as an event-sourced change, so it is the same for every management client and survives restarts. Every upload records who uploaded it and when.
- Until a template is uploaded, and after a reset, the default template is used.
- Templates cannot import Typst packages or read files other than the card data, so rendering never needs the network. A template that tries to is rejected.

## Acceptance criteria

- Readers and administrators can see whether the character-card template is the default or an uploaded one, and download the current template (to use as a starting point).
- Only administrators can upload a template or reset to the default. Reset asks for confirmation.
- An upload is checked by rendering it with sample card data before it is saved. A template that does not compile, uses a package, or reads another file is rejected, and the Typst error is shown. Nothing is saved on failure.
- Administrators can preview an uploaded file with sample data before saving it, using the MANAGER-13 preview behavior.
- Errors are clear and localized in English and Italian: invalid template, template too large, stale change, and persistence failures.
- Every change is an authorized, validated, auditable event-sourced command with optimistic concurrency.

## Verification

- xUnit tests cover template storage, reset, validation failures, authorization, event emission, and concurrency conflicts.
- Integration tests render the sample data with an uploaded template and confirm that a package import and a file read are rejected.
- Management-client tests cover upload, preview, reset with confirmation, download, and error display.
- Maintain localization, offline behavior, and the repository's coverage requirements.
