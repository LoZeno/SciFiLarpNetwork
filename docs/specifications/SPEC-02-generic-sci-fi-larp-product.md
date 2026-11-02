# SPEC-02: Generic Sci-Fi LARP product

## Requirement

Replace the Star Wars-specific “Holonet” product identity with a generic Sci-Fi LARP network-management platform. Product names, user-facing terminology, domain concepts, examples, sample data, and assets must not require a particular fictional franchise.

English is the default language of all applications. The web client, backend user-facing responses, and management application support English and Italian at minimum, with further languages addable through localization resources. See [SPEC-20](SPEC-20-language-and-localization.md).

Every application and core feature must also work on a closed intranet without guaranteed public internet access. See [SPEC-21](SPEC-21-offline-intranet-operation.md).

The canonical product identity, vocabulary, initial capability scope, and explicit omissions are documented in [Product language and initial capability scope](../architecture/domain-language-and-scope.md).

## Acceptance criteria

- User-facing product language is franchise-neutral and supports configurable setting-specific names and data where practical.
- Application UI and user-facing messages meet the English-default and English/Italian localization requirements in SPEC-20.
- Core application behavior meets the offline/closed-intranet requirements in SPEC-21.
- Domain names and seeded/sample content are generic; no legacy Star Wars-specific branding, copyrighted imagery, or setting assumptions are carried forward.
- The initial in-scope capabilities and intentionally omitted/materially changed legacy behavior are documented in the domain language and scope guide before feature implementation.
- Product and domain terminology follows the generic vocabulary in that guide; setting-specific display names are configurable and are not hard-coded franchise assumptions.
