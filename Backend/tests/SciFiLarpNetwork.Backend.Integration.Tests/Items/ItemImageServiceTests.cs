using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Backend.Tests.Integration.Networks;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

[Collection("Backend database integration")]
public sealed class ItemImageServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly ItemTestData data = new(factory);

    private static CancellationToken Token => ItemTestData.Token;

    private string ItemImagesFolder => Path.Combine(factory.MediaRootPath, "ItemImages");

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task UploadedPictureIsStoredInTheItemImagesFolder()
    {
        var item = await data.ItemAsync();

        var result = await data.Images.SetImageAsync(item.ItemId, item.Version, ItemTestImages.Png(), Token);

        Assert.True(File.Exists(Path.Combine(ItemImagesFolder, result.Value!.ImageId!.Value.ToString("D"))));
    }

    [Fact]
    public async Task ReplacedPictureIsStillServed()
    {
        var item = await data.ItemAsync();
        var first = (await data.Images.SetImageAsync(item.ItemId, item.Version, ItemTestImages.Png(), Token)).Value!;
        await data.Images.SetImageAsync(item.ItemId, first.Version, ItemTestImages.Png(2048), Token);

        var image = await data.Images.GetImageAsync(first.ImageId!.Value, Token);

        Assert.Equal(1024, image!.Content.Length);
    }

    [Fact]
    public async Task RemovedPictureLeavesTheItemWithoutOne()
    {
        var item = await data.ItemAsync();
        var withImage = (await data.Images.SetImageAsync(item.ItemId, item.Version, ItemTestImages.Png(), Token)).Value!;

        var result = await data.Images.RemoveImageAsync(item.ItemId, withImage.Version, Token);

        Assert.Null(result.Value!.ImageId);
    }

    [Fact]
    public async Task PictureLargerThanTwoMegabytesIsRefused()
    {
        var item = await data.ItemAsync();

        var result = await data.Images.SetImageAsync(item.ItemId, item.Version, ItemTestImages.Png(2 * 1024 * 1024 + 1), Token);

        Assert.Equal(ItemOutcome.ImageTooLarge, result.Outcome);
    }

    [Fact]
    public async Task ContentThatIsNoPictureIsRefused()
    {
        var item = await data.ItemAsync();

        var result = await data.Images.SetImageAsync(item.ItemId, item.Version, "<svg></svg>"u8.ToArray(), Token);

        Assert.Equal(ItemOutcome.InvalidImage, result.Outcome);
    }

    [Fact]
    public async Task StaleUploadLeavesNoFileBehind()
    {
        var item = await data.ItemAsync();
        await data.Items.ArchiveItemAsync(item.ItemId, Token);

        await data.Images.SetImageAsync(item.ItemId, item.Version, ItemTestImages.Png(), Token);

        Assert.False(Directory.Exists(ItemImagesFolder) && Directory.EnumerateFiles(ItemImagesFolder).Any());
    }

    [Fact]
    public async Task PictureWiderThanTheLimitIsRefused()
    {
        var item = await data.ItemAsync();

        var result = await data.Images.SetImageAsync(item.ItemId, item.Version,
            NetworkTestImages.Png(size: 100, NetworkImageFormats.MaximumPixelDimension + 1, 10), Token);

        Assert.Equal(ItemOutcome.ImageDimensionsTooLarge, result.Outcome);
    }

    [Fact]
    public async Task UnknownPictureIsNotFound()
    {
        Assert.Null(await data.Images.GetImageAsync(Guid.NewGuid(), Token));
    }
}
