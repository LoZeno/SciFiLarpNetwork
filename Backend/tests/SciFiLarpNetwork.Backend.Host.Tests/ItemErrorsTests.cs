using SciFiLarpNetwork.Backend.Api;
using SciFiLarpNetwork.Backend.Api.Items;
using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class ItemErrorsTests
{
    [Theory]
    [InlineData(ItemOutcome.NotFound, 404, "errors.notFound")]
    [InlineData(ItemOutcome.StaleVersion, 409, "errors.staleVersion")]
    [InlineData(ItemOutcome.NameAlreadyUsed, 409, "errors.nameAlreadyUsed")]
    [InlineData(ItemOutcome.ConcurrentChange, 409, "errors.concurrentChange")]
    [InlineData(ItemOutcome.DuplicateRecipe, 409, "errors.duplicateRecipe")]
    [InlineData(ItemOutcome.PrintIdInUse, 409, "errors.printIdInUse")]
    [InlineData(ItemOutcome.InvalidName, 400, "errors.invalidName")]
    [InlineData(ItemOutcome.InvalidDescription, 400, "errors.invalidDescription")]
    [InlineData(ItemOutcome.InvalidEffect, 400, "errors.invalidEffect")]
    [InlineData(ItemOutcome.InvalidCost, 400, "errors.invalidItemCost")]
    [InlineData(ItemOutcome.InvalidCharges, 400, "errors.invalidCharges")]
    [InlineData(ItemOutcome.UnknownItemType, 400, "errors.unknownItemType")]
    [InlineData(ItemOutcome.ItemTypeArchived, 400, "errors.itemTypeArchived")]
    [InlineData(ItemOutcome.UnknownRarity, 400, "errors.unknownRarity")]
    [InlineData(ItemOutcome.RarityArchived, 400, "errors.rarityArchived")]
    [InlineData(ItemOutcome.InvalidImage, 400, "errors.invalidImage")]
    [InlineData(ItemOutcome.ImageTooLarge, 400, "errors.imageTooLarge")]
    [InlineData(ItemOutcome.ImageDimensionsTooLarge, 400, "errors.imageDimensionsTooLarge")]
    [InlineData(ItemOutcome.InvalidQuantity, 400, "errors.invalidQuantity")]
    [InlineData(ItemOutcome.TooManyComponents, 400, "errors.tooManyComponents")]
    [InlineData(ItemOutcome.SelfComponent, 400, "errors.selfComponent")]
    [InlineData(ItemOutcome.ComponentAlreadyAdded, 400, "errors.componentAlreadyAdded")]
    [InlineData(ItemOutcome.NotAComponent, 400, "errors.notAComponent")]
    [InlineData(ItemOutcome.UnknownComponent, 400, "errors.unknownComponent")]
    [InlineData(ItemOutcome.ComponentArchived, 400, "errors.componentArchived")]
    [InlineData(ItemOutcome.ComponentLoop, 400, "errors.componentLoop")]
    [InlineData(ItemOutcome.UnknownCloneSource, 400, "errors.unknownCloneSource")]
    [InlineData(ItemOutcome.NoItemsSelected, 400, "errors.noItemsSelected")]
    [InlineData(ItemOutcome.InvalidCopyCount, 400, "errors.invalidCopyCount")]
    [InlineData(ItemOutcome.TooManyCards, 400, "errors.tooManyItemCards")]
    [InlineData(ItemOutcome.UnknownItems, 400, "errors.unknownItems")]
    [InlineData(ItemOutcome.ItemArchived, 400, "errors.itemArchived")]
    [InlineData(ItemOutcome.InvalidPrintId, 400, "errors.invalidPrintId")]
    [InlineData(ItemOutcome.NoCopiesSelected, 400, "errors.noCopiesSelected")]
    [InlineData(ItemOutcome.UnknownCopies, 400, "errors.unknownCopies")]
    public void OutcomeHasItsStableCode(ItemOutcome outcome, int statusCode, string code)
    {
        var exception = ItemErrors.ToApiException(outcome);

        Assert.Equal(new { StatusCode = statusCode, Code = code }, new { exception.StatusCode, exception.Code });
    }

    [Fact]
    public void SuccessIsNotAnError() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemErrors.ToApiException(ItemOutcome.Succeeded));

    [Fact]
    public void UnwrapReturnsTheValueOfASuccess() =>
        Assert.Equal("saved", ItemErrors.Unwrap(new ItemResult<string>(ItemOutcome.Succeeded, "saved", [])));

    [Fact]
    public void UnwrapListsTheUnknownCopies()
    {
        var copyId = Guid.NewGuid();

        var exception = Assert.Throws<ApiException>(() =>
            ItemErrors.Unwrap(new ItemResult<string>(ItemOutcome.UnknownCopies, null, [copyId])));

        Assert.Equal([copyId.ToString("D")], exception.Errors![ItemErrors.CopyIdsErrorKey]);
    }

    [Fact]
    public void RenderingFailureIsAServerError() =>
        Assert.Equal(500, ItemErrors.RenderingFailed().StatusCode);
}
