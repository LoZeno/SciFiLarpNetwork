# Manager tab: Substances and ingredients

**Legacy entry:** `HolonetManager` → “Sostanze & Ingredienti” (`HolonetWinControls\Sostanze\ControlSostanze`).

## Observed behavior

Filters by substance type, supports paged and text-filtered listing, shows components for a selected substance, and supports create, edit, clone, add/remove component, and printing ingredient or substance QR cards. A single selection may be repeated for multi-copy printing.

## Acceptance criteria

- Administrators can filter and search ingredients, medicines, toxins, drugs, and other configured substance categories without conflating their gameplay roles.
- Create/edit/clone covers applicable name, category, description/effect, usage, efficacy, cost, expiry, image, QR identifier, and component composition.
- Component changes validate composition and update the selected detail view; destructive removal requires confirmation.
- Printouts use the correct template for the selected category and contain an unambiguous QR code; multi-copy output does not create duplicate database entities.
- Invalid type selections, empty results, and persistence failures have clear states and cannot be reported as successful saves.
