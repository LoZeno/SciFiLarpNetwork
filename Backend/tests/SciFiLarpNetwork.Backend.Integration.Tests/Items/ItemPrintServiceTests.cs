using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

[Collection("Backend database integration")]
public sealed class ItemPrintServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly ItemTestData data = new(factory);

    private static CancellationToken Token => ItemTestData.Token;

    private IItemPrintService Prints => data.Prints;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PrintMakesTheRequestedCopiesOfEveryItem()
    {
        var medkit = await data.ItemAsync("Medkit");
        var grenade = await data.ItemAsync("Grenade");

        var result = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId, grenade.ItemId], 3, Token);

        Assert.Equal(["Medkit", "Medkit", "Medkit", "Grenade", "Grenade", "Grenade"], result.Value!.Cards.Select(card => card.Name));
    }

    [Fact]
    public async Task ItemListedTwiceIsPrintedForEachTime()
    {
        var medkit = await data.ItemAsync("Medkit");

        var result = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId, medkit.ItemId], 2, Token);

        Assert.Equal(4, result.Value!.Cards.Select(card => card.QrCode).Distinct().Count());
    }

    [Fact]
    public async Task PrintIdResentWithTheItemsInAnotherOrderIsRefused()
    {
        var medkit = await data.ItemAsync("Medkit");
        var grenade = await data.ItemAsync("Grenade");
        var printId = Guid.NewGuid();
        await Prints.PrintAsync(printId, [medkit.ItemId, grenade.ItemId], 1, Token);

        var result = await Prints.PrintAsync(printId, [grenade.ItemId, medkit.ItemId], 1, Token);

        Assert.Equal(ItemOutcome.PrintIdInUse, result.Outcome);
    }

    [Fact]
    public async Task EveryPrintedCardHasItsOwnQrCode()
    {
        var medkit = await data.ItemAsync();

        var result = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 10, Token);

        Assert.Equal(10, result.Value!.Cards.Select(card => card.QrCode).Distinct().Count());
    }

    [Fact]
    public async Task PrintedCopiesAreActiveAndListed()
    {
        var medkit = await data.ItemAsync();
        var printed = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 2, Token);

        var copies = await data.Copies.GetCopiesAsync(medkit.ItemId, Token);

        Assert.Equal(
            printed.Value!.Cards.Select(card => (card.QrCode, true)).Order(),
            copies!.Select(copy => (copy.QrCode, copy.IsActive)).Order());
    }

    [Fact]
    public async Task PrintingDoesNotChangeTheItem()
    {
        var medkit = await data.ItemAsync();

        await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 5, Token);

        Assert.Equal(medkit.Version, (await data.Items.GetItemAsync(medkit.ItemId, Token))!.Version);
    }

    [Fact]
    public async Task RepeatedPrintRequestReturnsTheSameCards()
    {
        var medkit = await data.ItemAsync();
        var printId = Guid.NewGuid();
        var first = await Prints.PrintAsync(printId, [medkit.ItemId], 2, Token);

        var second = await Prints.PrintAsync(printId, [medkit.ItemId], 2, Token);

        Assert.Equal(first.Value!.Cards, second.Value!.Cards);
    }

    [Fact]
    public async Task RepeatedPrintRequestMakesNoExtraCopies()
    {
        var medkit = await data.ItemAsync();
        var printId = Guid.NewGuid();
        await Prints.PrintAsync(printId, [medkit.ItemId], 2, Token);

        await Prints.PrintAsync(printId, [medkit.ItemId], 2, Token);

        Assert.Equal(2, (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!.Count);
    }

    [Fact]
    public async Task PrintIdWithDifferentContentIsRefused()
    {
        var medkit = await data.ItemAsync();
        var printId = Guid.NewGuid();
        await Prints.PrintAsync(printId, [medkit.ItemId], 2, Token);

        var result = await Prints.PrintAsync(printId, [medkit.ItemId], 3, Token);

        Assert.Equal(ItemOutcome.PrintIdInUse, result.Outcome);
    }

    [Fact]
    public async Task ConcurrentRepeatedPrintRequestsMakeOnePrint()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await factory.ResetAsync();
            var medkit = await data.ItemAsync();
            var printId = Guid.NewGuid();
            factory.Clock.FirstReadBarrier = new Barrier(2);

            await Task.WhenAll(Prints.PrintAsync(printId, [medkit.ItemId], 2, Token), Prints.PrintAsync(printId, [medkit.ItemId], 2, Token));

            Assert.Equal(2, (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!.Count);
        }
    }

    [Fact]
    public async Task ArchivedItemCannotBePrinted()
    {
        var medkit = await data.ArchivedItemAsync("Medkit");

        var result = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 1, Token);

        Assert.Equal(ItemOutcome.ItemArchived, result.Outcome);
    }

    [Fact]
    public async Task UnknownItemsAreListed()
    {
        var medkit = await data.ItemAsync();
        var unknown = Guid.NewGuid();

        var result = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId, unknown], 1, Token);

        Assert.Equal((ItemOutcome.UnknownItems, unknown.ToString()), (result.Outcome, string.Join(",", result.UnknownIds)));
    }

    [Theory]
    [InlineData(0, ItemOutcome.InvalidCopyCount)]
    [InlineData(51, ItemOutcome.InvalidCopyCount)]
    public async Task CopyCountOutsideTheRangeIsRefused(int copies, ItemOutcome expected)
    {
        var medkit = await data.ItemAsync();

        var result = await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], copies, Token);

        Assert.Equal(expected, result.Outcome);
    }

    [Fact]
    public async Task MoreThanTwoHundredCardsAreRefused()
    {
        var result = await Prints.PrintAsync(Guid.NewGuid(), [.. Enumerable.Range(0, 5).Select(_ => Guid.NewGuid())], 41, Token);

        Assert.Equal(ItemOutcome.TooManyCards, result.Outcome);
    }

    [Fact]
    public async Task EmptySelectionIsRefused()
    {
        var result = await Prints.PrintAsync(Guid.NewGuid(), [], 1, Token);

        Assert.Equal(ItemOutcome.NoItemsSelected, result.Outcome);
    }

    [Fact]
    public async Task EmptyPrintIdIsRefused()
    {
        var medkit = await data.ItemAsync();

        var result = await Prints.PrintAsync(Guid.Empty, [medkit.ItemId], 1, Token);

        Assert.Equal(ItemOutcome.InvalidPrintId, result.Outcome);
    }

    [Fact]
    public async Task ReprintUsesTheSameQrCodes()
    {
        var medkit = await data.ItemAsync();
        await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 2, Token);
        var copies = (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!;

        var result = await Prints.ReprintAsync([.. copies.Select(copy => copy.CopyId)], Token);

        Assert.Equal(copies.Select(copy => copy.QrCode), result.Value!.Cards.Select(card => card.QrCode));
    }

    [Fact]
    public async Task ReprintMakesNoCopies()
    {
        var medkit = await data.ItemAsync();
        await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 2, Token);
        var copies = (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!;

        await Prints.ReprintAsync([copies[0].CopyId], Token);

        Assert.Equal(2, (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!.Count);
    }

    [Fact]
    public async Task ReprintOfAnArchivedItemIsAllowed()
    {
        var medkit = await data.ItemAsync();
        await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 1, Token);
        var copy = (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!.Single();
        await data.Items.ArchiveItemAsync(medkit.ItemId, Token);

        var result = await Prints.ReprintAsync([copy.CopyId], Token);

        Assert.True(result.Value!.Cards.Single().IsArchived);
    }

    [Fact]
    public async Task ReprintOfUnknownCopiesListsThem()
    {
        var unknown = Guid.NewGuid();

        var result = await Prints.ReprintAsync([unknown], Token);

        Assert.Equal((ItemOutcome.UnknownCopies, unknown.ToString()), (result.Outcome, string.Join(",", result.UnknownIds)));
    }

    [Fact]
    public async Task EmptyReprintIsRefused()
    {
        var result = await Prints.ReprintAsync([], Token);

        Assert.Equal(ItemOutcome.NoCopiesSelected, result.Outcome);
    }

    [Fact]
    public async Task PreviewUsesTheSampleQrCode()
    {
        var medkit = await data.ItemAsync();

        var result = await Prints.PreviewAsync([medkit.ItemId], 2, Token);

        Assert.Equal([IItemPrintService.SampleQrCode, IItemPrintService.SampleQrCode], result.Value!.Cards.Select(card => card.QrCode));
    }

    [Fact]
    public async Task PreviewMakesNoCopies()
    {
        var medkit = await data.ItemAsync();

        await Prints.PreviewAsync([medkit.ItemId], 2, Token);

        Assert.Empty((await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!);
    }

    [Fact]
    public async Task CardCarriesTheItemDetailsAndTypeName()
    {
        var medkit = await data.ItemAsync();

        var result = await Prints.PreviewAsync([medkit.ItemId], 1, Token);

        Assert.Equal(
            new ItemCard("Type of Medkit", "Medkit", IItemPrintService.SampleQrCode, "A small kit.", "Heals two wounds.", 3,
                new DateOnly(2027, 5, 1), false),
            result.Value!.Cards.Single());
    }

    [Fact]
    public async Task DeactivatedCopyIsInactive()
    {
        var copy = await PrintOneCopyAsync();

        var result = await data.Copies.DeactivateCopyAsync(copy.CopyId, Token);

        Assert.Equal(new { Outcome = ItemOutcome.Succeeded, IsActive = false }, new { result.Outcome, result.Value!.IsActive });
    }

    [Fact]
    public async Task ReactivatedCopyIsActiveAgain()
    {
        var copy = await PrintOneCopyAsync();
        await data.Copies.DeactivateCopyAsync(copy.CopyId, Token);

        var result = await data.Copies.ReactivateCopyAsync(copy.CopyId, Token);

        Assert.True(result.Value!.IsActive);
    }

    [Fact]
    public async Task DeactivationRecordsWhen()
    {
        var copy = await PrintOneCopyAsync();
        factory.Clock.Advance(TimeSpan.FromHours(1));

        var result = await data.Copies.DeactivateCopyAsync(copy.CopyId, Token);

        Assert.True(result.Value!.ChangedAtUtc >= copy.PrintedAtUtc.AddHours(1));
    }

    [Fact]
    public async Task DeactivatingAnUnknownCopyIsNotFound()
    {
        var result = await data.Copies.DeactivateCopyAsync(Guid.NewGuid(), Token);

        Assert.Equal(ItemOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task CopiesOfAnUnknownItemAreNull()
    {
        Assert.Null(await data.Copies.GetCopiesAsync(Guid.NewGuid(), Token));
    }

    [Fact]
    public async Task CopiesAreListedNewestPrintFirst()
    {
        var medkit = await data.ItemAsync();
        var firstPrint = Guid.NewGuid();
        var secondPrint = Guid.NewGuid();
        await Prints.PrintAsync(firstPrint, [medkit.ItemId], 1, Token);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await Prints.PrintAsync(secondPrint, [medkit.ItemId], 1, Token);

        var copies = await data.Copies.GetCopiesAsync(medkit.ItemId, Token);

        Assert.Equal([secondPrint, firstPrint], copies!.Select(copy => copy.PrintId));
    }

    private async Task<ItemCopySummary> PrintOneCopyAsync()
    {
        var medkit = await data.ItemAsync();
        await Prints.PrintAsync(Guid.NewGuid(), [medkit.ItemId], 1, Token);
        return (await data.Copies.GetCopiesAsync(medkit.ItemId, Token))!.Single();
    }
}
