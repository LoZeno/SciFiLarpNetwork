using System.Net;
using System.Text.Json;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Networks;

[Collection("Backend database integration")]
public sealed class NetworkApiTests : IAsyncLifetime
{
    private const string NetworksPath = "/local/api/v1/networks";
    private readonly AdministratorAuthenticationFactory _factory = new();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task VersionRootAdvertisesNetworksToReaders()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1", token);

        Assert.Equal(NetworksPath, (await ReadAsync(response)).LinkHref(SirenRelations.Networks));
    }

    [Fact]
    public async Task AnonymousRequestCannotListNetworks()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(NetworksPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCanListNetworksWithoutWriteActions()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, Actions = "" },
            new { Status = response.StatusCode, Actions = string.Join(',', (await ReadAsync(response)).ActionNames()) });
    }

    [Fact]
    public async Task AdministratorSeesTheCreateNetworkAction()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);

        Assert.Equal(["create-network"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task ReaderCannotCreateNetworks()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, NetworksPath, token, new { code = "rim", name = "Outer Rim", theme = "blue" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorCreatesANetwork()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, NetworksPath, token, new { code = " Outer-Rim ", name = "Outer Rim", theme = "amber" });
        var network = await ReadAsync(response);

        Assert.Equal(
            new { Status = HttpStatusCode.Created, Code = (string?)"outer-rim", Theme = (string?)"amber", Enabled = true, Default = false },
            new
            {
                Status = response.StatusCode,
                Code = network.Property("code").GetString(),
                Theme = network.Property("theme").GetString(),
                Enabled = network.Property("isEnabled").GetBoolean(),
                Default = network.Property("isDefault").GetBoolean()
            });
    }

    [Fact]
    public async Task DuplicateCodeReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, NetworksPath, token, new { code = "RIM", name = "Inner Rim", theme = "blue" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.codeAlreadyUsed"), await ErrorAsync(response));
    }

    [Fact]
    public async Task MalformedCodeReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, NetworksPath, token, new { code = "outer rim", name = "Outer Rim", theme = "blue" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidCode"), await ErrorAsync(response));
    }

    [Theory]
    [MemberData(nameof(InvalidThemes))]
    public async Task UnknownOrNumericThemesAreRejected(object theme)
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, NetworksPath, token, new { code = "rim", name = "Outer Rim", theme });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.validation"), await ErrorAsync(response));
    }

    public static TheoryData<object> InvalidThemes => new() { "gray", 1, "1" };

    [Fact]
    public async Task NetworkCollectionOffersEveryLocalizedTheme()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token, "it");

        Assert.Equal(
            ["blue:Blu", "red:Rosso", "green:Verde", "yellow:Giallo", "amber:Ambra", "grey:Grigio", "purple:Viola"],
            (await ReadAsync(response)).Property("themes").EnumerateArray()
                .Select(theme => $"{theme.GetProperty("name").GetString()}:{theme.GetProperty("title").GetString()}"));
    }

    [Fact]
    public async Task EmptyCatalogReportsNoDefault()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);

        Assert.False((await ReadAsync(response)).Property("hasDefault").GetBoolean());
    }

    [Fact]
    public async Task MakingADefaultIsReportedByTheCollection()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        (await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/make-default")).Dispose();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);

        Assert.True((await ReadAsync(response)).Property("hasDefault").GetBoolean());
    }

    [Fact]
    public async Task EnabledNetworkOffersEditImageDefaultAndDisable()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{NetworksPath}/{networkId:D}", token);

        Assert.Equal(
            ["update-network", "set-background-image", "make-default-network", "disable-network"],
            (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task DefaultNetworkOffersNeitherDisableNorMakeDefault()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/make-default");

        Assert.Equal(["update-network", "set-background-image"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task DisabledNetworkOffersEnable()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/disable");

        Assert.Equal(["update-network", "set-background-image", "enable-network"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task DisablingTheDefaultReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        (await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/make-default")).Dispose();

        using var response = await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/disable");

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.defaultNetworkCannotBeDisabled"), await ErrorAsync(response));
    }

    [Fact]
    public async Task MakingADisabledNetworkDefaultReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        (await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/disable")).Dispose();

        using var response = await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/make-default");

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.networkDisabled"), await ErrorAsync(response));
    }

    [Fact]
    public async Task DisabledNetworksAreReachedThroughTheirLink()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        (await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/disable")).Dispose();
        using var active = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);
        var withDisabled = (await ReadAsync(active)).LinkHref(SirenRelations.WithDisabled)!;

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, withDisabled, token);

        Assert.Equal(["rim"], (await ReadAsync(response)).EntityCodes());
    }

    [Fact]
    public async Task DisabledNetworksAreHiddenByDefault()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        (await PostAsync(client, token, $"{NetworksPath}/{networkId:D}/disable")).Dispose();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);

        Assert.Empty((await ReadAsync(response)).EntityCodes());
    }

    [Fact]
    public async Task StaleEditReturnsTheStaleVersionCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        (await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}", token, new { version = 1, name = "Far Rim", theme = "red" })).Dispose();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}", token, new { version = 1, name = "Near Rim", theme = "red" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.staleVersion"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownNetworkReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{NetworksPath}/{Guid.NewGuid():D}", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadedImageIsServedWithItsMediaType()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        var content = NetworkTestImages.Png(size: 2048);
        var imageHref = await UploadImageAsync(client, token, networkId, content);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, imageHref, token);

        Assert.Equal(
            new ImageSnapshot(HttpStatusCode.OK, "image/png", "nosniff", content),
            new ImageSnapshot(
                response.StatusCode,
                response.Content.Headers.ContentType?.MediaType,
                string.Join(',', response.Headers.GetValues("X-Content-Type-Options")),
                await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task UploadedImageIsLinkedFromTheNetworkList()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        var imageHref = await UploadImageAsync(client, token, networkId, NetworkTestImages.Png(size: 100));

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, NetworksPath, token);

        Assert.Equal(imageHref, (await ReadAsync(response)).FirstEntityLinkHref(SirenRelations.BackgroundImage));
    }

    [Fact]
    public async Task ReaderCanDownloadImages()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _) = await _factory.PrepareReaderWithTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, adminToken, "rim", "Outer Rim");
        var imageHref = await UploadImageAsync(client, adminToken, networkId, NetworkTestImages.Png(size: 100));
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, imageHref, readerToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequestCannotDownloadImages()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        var imageHref = await UploadImageAsync(client, token, networkId, NetworkTestImages.Png(size: 100));

        using var response = await client.GetAsync(imageHref, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCannotUploadImages()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _) = await _factory.PrepareReaderWithTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, adminToken, "rim", "Outer Rim");
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}/background-image", readerToken,
            new { version = 1, content = NetworkTestImages.Png(size: 100) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SvgUploadReturnsTheInvalidImageCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}/background-image", token,
            new { version = 1, content = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray() });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidImage"), await ErrorAsync(response));
    }

    [Fact]
    public async Task ImageWithTooManyPixelsReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}/background-image", token,
            new { version = 1, content = NetworkTestImages.Png(size: 100, width: 60000, height: 60000) });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.imageDimensionsTooLarge"), await ErrorAsync(response));
    }

    [Fact]
    public async Task OversizedUploadReturnsTheImageTooLargeCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}/background-image", token,
            new { version = 1, content = NetworkTestImages.Png(NetworkImageFormats.MaximumByteCount + 1) });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.imageTooLarge"), await ErrorAsync(response));
    }

    [Fact]
    public async Task RemovingTheImageRemovesItsLink()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var networkId = await CreateNetworkAsync(client, token, "rim", "Outer Rim");
        await UploadImageAsync(client, token, networkId, NetworkTestImages.Png(size: 100));

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Delete, $"{NetworksPath}/{networkId:D}/background-image", token, new { version = 2 });

        Assert.Null((await ReadAsync(response)).LinkHrefOrNull(SirenRelations.BackgroundImage));
    }

    [Fact]
    public async Task UnknownImageReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(
            client, HttpMethod.Get, $"/local/api/v1/network-images/{Guid.NewGuid():D}", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await _factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    private async Task<string> ReaderTokenAsync(HttpClient client)
    {
        await _factory.CreateReaderAccountAsync(client);
        return await AdministratorAuthenticationFactory.SignInReaderAsync(client);
    }

    private static async Task<Guid> CreateNetworkAsync(HttpClient client, string token, string code, string name)
    {
        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, NetworksPath, token, new { code, name, theme = "grey" });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).Property("networkId").GetGuid();
    }

    private static async Task<string> UploadImageAsync(HttpClient client, string token, Guid networkId, byte[] content)
    {
        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{NetworksPath}/{networkId:D}/background-image", token, new { version = 1, content });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).LinkHref(SirenRelations.BackgroundImage)!;
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string path) =>
        AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, path, token, new { });

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone());

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await ReadAsync(response)).Property("code").GetString());

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code);

    private sealed record ImageSnapshot(HttpStatusCode Status, string? MediaType, string ContentTypeOptions, byte[] Content)
    {
        public bool Equals(ImageSnapshot? other) =>
            other is not null &&
            (Status, MediaType, ContentTypeOptions) == (other.Status, other.MediaType, other.ContentTypeOptions) &&
            Content.AsSpan().SequenceEqual(other.Content);

        public override int GetHashCode() => HashCode.Combine(Status, MediaType, ContentTypeOptions, Content.Length);
    }

    private sealed record SirenDocument(JsonElement Root)
    {
        public JsonElement Property(string name) => Root.GetProperty("properties").GetProperty(name);

        public string? LinkHref(string rel) => LinkHrefOrNull(rel) ?? throw new InvalidOperationException($"No {rel} link.");

        public string? LinkHrefOrNull(string rel) => FindLinkHref(Root, rel);

        public string? FirstEntityLinkHref(string rel) => FindLinkHref(Root.GetProperty("entities")[0], rel);

        public string?[] ActionNames() =>
            Root.TryGetProperty("actions", out var actions)
                ? actions.EnumerateArray().Select(action => action.GetProperty("name").GetString()).ToArray()
                : [];

        public string?[] EntityCodes() =>
            Root.GetProperty("entities").EnumerateArray()
                .Select(entity => entity.GetProperty("properties").GetProperty("code").GetString())
                .ToArray();

        private static string? FindLinkHref(JsonElement entity, string rel) =>
            entity.TryGetProperty("links", out var links)
                ? links.EnumerateArray()
                    .Where(link => link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == rel))
                    .Select(link => link.GetProperty("href").GetString())
                    .FirstOrDefault()
                : null;
    }
}
