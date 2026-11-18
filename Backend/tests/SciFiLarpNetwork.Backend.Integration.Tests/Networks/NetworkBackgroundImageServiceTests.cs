using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Networks;

[Collection("Backend database integration")]
public sealed class NetworkBackgroundImageServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private INetworkService Networks => factory.Services.GetRequiredService<INetworkService>();

    private INetworkBackgroundImageService Images => factory.Services.GetRequiredService<INetworkBackgroundImageService>();

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task StoredImageBytesRoundTripUnchanged()
    {
        var network = await CreateNetworkAsync();
        var content = NetworkTestImages.Png(size: 4096);
        var saved = await Images.SetBackgroundImageAsync(network.NetworkId, network.Version, content, TestContext.Current.CancellationToken);

        var image = await Images.GetImageAsync(saved.Value!.BackgroundImage!.ImageId, TestContext.Current.CancellationToken);

        Assert.Equal(content, image!.Content);
    }

    [Fact]
    public async Task UploadedImageIsReferencedByTheNetwork()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Jpeg(size: 100), TestContext.Current.CancellationToken);

        Assert.Equal(
            new { MediaType = "image/jpeg", ByteCount = 100, Version = 2L },
            new { result.Value!.BackgroundImage!.MediaType, result.Value.BackgroundImage.ByteCount, result.Value.Version });
    }

    [Fact]
    public async Task ImageAtTheSizeLimitIsAccepted()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Png(NetworkImageFormats.MaximumByteCount), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task ImageOverTheSizeLimitIsRejected()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Png(NetworkImageFormats.MaximumByteCount + 1), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.ImageTooLarge, result.Outcome);
    }

    [Theory]
    [InlineData(NetworkImageFormats.MaximumPixelDimension + 1, 10)]
    [InlineData(10, NetworkImageFormats.MaximumPixelDimension + 1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task ImageWithTooManyPixelsIsRejected(int width, int height)
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Png(size: 100, width, height), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.ImageDimensionsTooLarge, result.Outcome);
    }

    [Fact]
    public async Task ImageAtThePixelLimitIsAccepted()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId,
            network.Version,
            NetworkTestImages.Png(size: 100, NetworkImageFormats.MaximumPixelDimension, NetworkImageFormats.MaximumPixelDimension),
            TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task ImageWithASignatureButNoReadableHeaderIsRejected()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00], TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.InvalidImage, result.Outcome);
    }

    [Fact]
    public async Task SvgIsRejected()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.InvalidImage, result.Outcome);
    }

    [Fact]
    public async Task StaleImageUploadIsRejected()
    {
        var network = await CreateNetworkAsync();
        await Networks.UpdateNetworkAsync(network.NetworkId, network.Version, new NetworkDefinition("Rebels", "Changed", NetworkTheme.Red), TestContext.Current.CancellationToken);

        var result = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Png(size: 100), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task UploadToAnUnknownNetworkIsNotFound()
    {
        var result = await Images.SetBackgroundImageAsync(Guid.NewGuid(), 1, NetworkTestImages.Png(size: 100), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task RemovingTheImageRestoresTheThemeImage()
    {
        var network = await CreateNetworkAsync();
        var withImage = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Png(size: 100), TestContext.Current.CancellationToken);

        var result = await Images.RemoveBackgroundImageAsync(network.NetworkId, withImage.Value!.Version, TestContext.Current.CancellationToken);

        Assert.Null(result.Value!.BackgroundImage);
    }

    [Fact]
    public async Task RemovedImageIsKeptForHistory()
    {
        var network = await CreateNetworkAsync();
        var withImage = await Images.SetBackgroundImageAsync(
            network.NetworkId, network.Version, NetworkTestImages.Png(size: 100), TestContext.Current.CancellationToken);
        await Images.RemoveBackgroundImageAsync(network.NetworkId, withImage.Value!.Version, TestContext.Current.CancellationToken);

        var image = await Images.GetImageAsync(withImage.Value.BackgroundImage!.ImageId, TestContext.Current.CancellationToken);

        Assert.NotNull(image);
    }

    [Fact]
    public async Task RemovingWhenThereIsNoImageChangesNothing()
    {
        var network = await CreateNetworkAsync();

        var result = await Images.RemoveBackgroundImageAsync(network.NetworkId, network.Version, TestContext.Current.CancellationToken);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task StaleImageRemovalIsRejected()
    {
        var network = await CreateNetworkAsync();
        await Images.SetBackgroundImageAsync(network.NetworkId, network.Version, NetworkTestImages.Png(size: 100), TestContext.Current.CancellationToken);

        var result = await Images.RemoveBackgroundImageAsync(network.NetworkId, network.Version, TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task UnknownImageIsNotFound()
    {
        Assert.Null(await Images.GetImageAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    private async Task<NetworkSummary> CreateNetworkAsync() =>
        (await Networks.CreateNetworkAsync("rebels", new NetworkDefinition("Rebels", null, NetworkTheme.Grey), TestContext.Current.CancellationToken)).Value!;
}
