# SPEC-32: Item-card templates


## Purpose and scope

Let administrators replace the look of printed item cards with their own [Typst](https://typst.app) template, as [SPEC-29](SPEC-29-card-templates.md) does for character cards. [MANAGER-06](legacy-features/manager-tabs/MANAGER-06-items-components.md) ships the default item-card template and the item-card data; this specification adds uploading, checking, previewing, and resetting an item-card template.

Split from MANAGER-06 by the user on 2026-11-25.


## Product model

- Every SPEC-29 rule applies to the **item card** kind, with its own template: storage as an event-sourced change, the 256 KB limit, the empty-file refusal, the check with sample data before saving, the ban on packages and other files, preview before saving, download, and reset to the default.
- The item-card template reads the item-card data and labels documented by MANAGER-06.
- Uploading or resetting one kind's template never changes another kind's template.


## Acceptance criteria

- The **Card templates** screen in the Management application lists **Item cards** next to **Character cards**, as **Default** or **Uploaded by** *name*, *date and time*, with the same actions and rights as character cards.
- An item-card upload is checked with sample item cards: one with every field set, one with very long names and text, one with no optional field, and one of an archived item.
- Printing item cards uses the uploaded item-card template, and the default after a reset.
- Errors, localization, authorization, auditing and concurrency follow SPEC-29.


## Verification

- xUnit and integration tests cover the item-card kind as SPEC-29 does for character cards, and show that changing one kind leaves the other unchanged.
- Management-client tests cover the second row of the Card templates screen.
