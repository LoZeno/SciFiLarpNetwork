using SciFiLarpNetwork.Backend.Api.CharacterCards;
using SciFiLarpNetwork.Backend.Api.Networks;
using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>Maps item command outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class ItemErrors
{
    public const string InvalidEffect = "errors.invalidEffect";
    public const string InvalidItemCost = "errors.invalidItemCost";
    public const string InvalidCharges = "errors.invalidCharges";
    public const string UnknownItemType = "errors.unknownItemType";
    public const string ItemTypeArchived = "errors.itemTypeArchived";
    public const string UnknownRarity = "errors.unknownRarity";
    public const string RarityArchived = "errors.rarityArchived";
    public const string InvalidQuantity = "errors.invalidQuantity";
    public const string TooManyComponents = "errors.tooManyComponents";
    public const string SelfComponent = "errors.selfComponent";
    public const string ComponentAlreadyAdded = "errors.componentAlreadyAdded";
    public const string NotAComponent = "errors.notAComponent";
    public const string UnknownComponent = "errors.unknownComponent";
    public const string ComponentArchived = "errors.componentArchived";
    public const string ComponentLoop = "errors.componentLoop";
    public const string DuplicateRecipe = "errors.duplicateRecipe";
    public const string UnknownCloneSource = "errors.unknownCloneSource";
    public const string NoItemsSelected = "errors.noItemsSelected";
    public const string InvalidCopyCount = "errors.invalidCopyCount";
    public const string TooManyItemCards = "errors.tooManyItemCards";
    public const string UnknownItems = "errors.unknownItems";
    public const string ItemArchived = "errors.itemArchived";
    public const string InvalidPrintId = "errors.invalidPrintId";
    public const string PrintIdInUse = "errors.printIdInUse";
    public const string NoCopiesSelected = "errors.noCopiesSelected";
    public const string UnknownCopies = "errors.unknownCopies";

    /// <summary>The error detail listing the requested items that do not exist.</summary>
    public const string ItemIdsErrorKey = "itemIds";

    /// <summary>The error detail listing the requested copies that do not exist.</summary>
    public const string CopyIdsErrorKey = "copyIds";

    public static ApiException ToApiException(ItemOutcome outcome) =>
        outcome switch
        {
            ItemOutcome.NotFound => Create(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound),
            ItemOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, CatalogErrors.StaleVersion),
            ItemOutcome.NameAlreadyUsed => Create(StatusCodes.Status409Conflict, CatalogErrors.NameAlreadyUsed),
            ItemOutcome.ConcurrentChange => Create(StatusCodes.Status409Conflict, CatalogErrors.ConcurrentChange),
            ItemOutcome.DuplicateRecipe => Create(StatusCodes.Status409Conflict, DuplicateRecipe),
            ItemOutcome.PrintIdInUse => Create(StatusCodes.Status409Conflict, PrintIdInUse),
            ItemOutcome.InvalidName => BadRequest(CatalogErrors.InvalidName),
            ItemOutcome.InvalidDescription => BadRequest(CatalogErrors.InvalidDescription),
            ItemOutcome.InvalidEffect => BadRequest(InvalidEffect),
            ItemOutcome.InvalidCost => BadRequest(InvalidItemCost),
            ItemOutcome.InvalidCharges => BadRequest(InvalidCharges),
            ItemOutcome.UnknownItemType => BadRequest(UnknownItemType),
            ItemOutcome.ItemTypeArchived => BadRequest(ItemTypeArchived),
            ItemOutcome.UnknownRarity => BadRequest(UnknownRarity),
            ItemOutcome.RarityArchived => BadRequest(RarityArchived),
            ItemOutcome.InvalidImage => BadRequest(NetworkErrors.InvalidImage),
            ItemOutcome.ImageTooLarge => BadRequest(NetworkErrors.ImageTooLarge),
            ItemOutcome.ImageDimensionsTooLarge => BadRequest(NetworkErrors.ImageDimensionsTooLarge),
            ItemOutcome.InvalidQuantity => BadRequest(InvalidQuantity),
            ItemOutcome.TooManyComponents => BadRequest(TooManyComponents),
            ItemOutcome.SelfComponent => BadRequest(SelfComponent),
            ItemOutcome.ComponentAlreadyAdded => BadRequest(ComponentAlreadyAdded),
            ItemOutcome.NotAComponent => BadRequest(NotAComponent),
            ItemOutcome.UnknownComponent => BadRequest(UnknownComponent),
            ItemOutcome.ComponentArchived => BadRequest(ComponentArchived),
            ItemOutcome.ComponentLoop => BadRequest(ComponentLoop),
            ItemOutcome.UnknownCloneSource => BadRequest(UnknownCloneSource),
            ItemOutcome.NoItemsSelected => BadRequest(NoItemsSelected),
            ItemOutcome.InvalidCopyCount => BadRequest(InvalidCopyCount),
            ItemOutcome.TooManyCards => BadRequest(TooManyItemCards),
            ItemOutcome.UnknownItems => BadRequest(UnknownItems),
            ItemOutcome.ItemArchived => BadRequest(ItemArchived),
            ItemOutcome.InvalidPrintId => BadRequest(InvalidPrintId),
            ItemOutcome.NoCopiesSelected => BadRequest(NoCopiesSelected),
            ItemOutcome.UnknownCopies => BadRequest(UnknownCopies),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static T Unwrap<T>(ItemResult<T> result)
        where T : class =>
        result switch
        {
            { Outcome: ItemOutcome.Succeeded, Value: { } value } => value,
            { Outcome: ItemOutcome.UnknownItems } => throw WithIds(UnknownItems, ItemIdsErrorKey, result.UnknownIds),
            { Outcome: ItemOutcome.UnknownCopies } => throw WithIds(UnknownCopies, CopyIdsErrorKey, result.UnknownIds),
            _ => throw ToApiException(result.Outcome)
        };

    public static ApiException RenderingFailed() => CharacterCardErrors.RenderingFailed();

    private static ApiException WithIds(string code, string key, IReadOnlyList<Guid> ids) =>
        new(StatusCodes.Status400BadRequest, code, code,
            new Dictionary<string, IReadOnlyList<string>> { [key] = [.. ids.Select(id => id.ToString("D"))] });

    private static ApiException BadRequest(string code) => Create(StatusCodes.Status400BadRequest, code);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
