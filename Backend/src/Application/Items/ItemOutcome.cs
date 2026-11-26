namespace SciFiLarpNetwork.Backend.Application.Items;

public enum ItemOutcome
{
    Succeeded,
    NotFound,
    StaleVersion,
    InvalidName,
    NameAlreadyUsed,
    InvalidDescription,
    InvalidEffect,
    InvalidCost,
    InvalidCharges,
    UnknownItemType,
    ItemTypeArchived,
    UnknownRarity,
    RarityArchived,
    InvalidImage,
    ImageTooLarge,
    ImageDimensionsTooLarge,
    InvalidQuantity,
    TooManyComponents,
    SelfComponent,
    ComponentAlreadyAdded,
    NotAComponent,
    UnknownComponent,
    ComponentArchived,
    ComponentLoop,
    DuplicateRecipe,
    UnknownCloneSource,
    NoItemsSelected,
    InvalidCopyCount,
    TooManyCards,
    UnknownItems,
    ItemArchived,
    InvalidPrintId,
    PrintIdInUse,
    NoCopiesSelected,
    UnknownCopies,

    /// <summary>
    /// Another command changed shared data first (the item composition, a component, a type or a rarity), but not the
    /// edited item itself; nothing was saved, and saving again re-checks every rule.
    /// </summary>
    ConcurrentChange
}
