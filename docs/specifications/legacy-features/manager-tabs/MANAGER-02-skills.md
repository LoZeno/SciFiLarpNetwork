# Manager tab: Skill groups and skills

**Legacy entry:** `HolonetManager` → “Liste & Abilità” (`HolonetWinControls\Abilita\ControlAbilita`).

## Observed behavior

Lists skill groups (“attitudini”), displays skills for the selected group, and opens separate create/edit dialogs for groups and skills.

## Acceptance criteria

- Administrators can list and select generic skill categories and view only skills belonging to the selected category.
- Administrators can create and edit categories and skills, including their name, description, progression/level, prerequisites, and any gameplay effect fields supported by the approved domain model.
- Validation prevents blank names, invalid prerequisite references, and duplicate identifiers; edits preserve unrelated relationships.
- Deleting or deactivating a category/skill with existing character/event references requires an explicit safe policy and confirmation.
- Updates are persisted and reflected in both lists without stale selection or accidental cross-category edits.
