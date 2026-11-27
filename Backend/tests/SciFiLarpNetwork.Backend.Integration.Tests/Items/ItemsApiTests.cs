using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;
using UglyToad.PdfPig;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Items;

[Collection("Backend database integration")]
public sealed class ItemsApiTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private const string ItemsPath = "/local/api/v1/items";
    private const string ItemTypesPath = "/local/api/v1/item-types";
    private const string RaritiesPath = "/local/api/v1/rarities";
    private const string CardsPath = "/local/api/v1/item-cards";

    private static readonly string[] FrontAndBackWords =
        ["TYPE", "Medkit", "Description:", "kit.", "Effect:", "wounds.", "Charges:", "3", "Expires:", "2027-05-01"];

    private static CancellationToken Token => ItemTestData.Token;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task VersionRootAdvertisesItemsToReaders()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1", token);

        Assert.Contains(ItemsPath, Hrefs(await JsonAsync(response), "links"));
    }

    [Fact]
    public async Task AnonymousRequestCannotListItems()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ItemsPath, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderSeesOnlyTheSearchAndPreviewActions()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, ItemsPath, token);

        Assert.Equal(["search-items", "preview-item-cards"], ActionNames(await JsonAsync(response)));
    }

    [Fact]
    public async Task AdministratorSeesTheWriteActions()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, ItemsPath, token);

        Assert.Equal(
            ["search-items", "preview-item-cards", "create-item", "print-item-cards", "reprint-item-cards", "reprint-item-print"],
            ActionNames(await JsonAsync(response)));
    }

    [Fact]
    public async Task ReaderCannotCreateItems()
    {
        using var client = factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, ItemTypesPath, token,
            new { name = "Tool" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorCreatesAnItem()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await CreateItemAsync(client, token, "Medkit");
        var item = await JsonAsync(response);

        Assert.Equal((HttpStatusCode.Created, "Medkit", 1L),
            (response.StatusCode, Property(item, "name").GetString(), Property(item, "version").GetInt64()));
    }

    [Fact]
    public async Task ItemOffersTheEditActions()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await CreateItemAsync(client, token, "Medkit");

        Assert.Equal(["update-item", "archive-item", "add-item-component", "set-item-image"], ActionNames(await JsonAsync(response)));
    }

    [Fact]
    public async Task DuplicateRecipeReturnsAStableConflictCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var cellId = await ItemIdAsync(client, token, "Power cell");
        (await CreateItemAsync(client, token, "Blaster", [new { itemId = cellId, quantity = 2 }])).Dispose();

        using var response = await CreateItemAsync(client, token, "Rifle", [new { itemId = cellId, quantity = 2 }]);

        Assert.Equal((HttpStatusCode.Conflict, "errors.duplicateRecipe"), await ErrorAsync(response));
    }

    [Fact]
    public async Task InvalidCostReturnsTheItemCostCode()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await CreateItemAsync(client, token, "Medkit", cost: 1_000_000);

        Assert.Equal((HttpStatusCode.BadRequest, "errors.invalidItemCost"), await ErrorAsync(response));
    }

    [Fact]
    public async Task ComponentIsOfferedQuantityAndRemovalActions()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var cellId = await ItemIdAsync(client, token, "Power cell");

        using var response = await CreateItemAsync(client, token, "Blaster", [new { itemId = cellId, quantity = 2 }]);

        Assert.Equal(["change-component-quantity", "remove-item-component"],
            ActionNames((await JsonAsync(response)).GetProperty("entities")[0]));
    }

    [Fact]
    public async Task RemovingAComponentThroughTheApiEmptiesTheRecipe()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var cellId = await ItemIdAsync(client, token, "Power cell");
        var blasterId = await ItemIdAsync(client, token, "Blaster", [new { itemId = cellId, quantity = 2 }]);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Delete,
            $"{ItemsPath}/{blasterId:D}/components/{cellId:D}", token, new { version = 1 });

        Assert.Equal(0, Property(await JsonAsync(response), "components").GetArrayLength());
    }

    [Fact]
    public async Task SearchFiltersTheList()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await ItemIdAsync(client, token, "Medkit");
        await ItemIdAsync(client, token, "Grenade");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get,
            $"{ItemsPath}?search=gren", token);

        Assert.Equal(["Grenade"], EntityNames(await JsonAsync(response)));
    }

    [Fact]
    public async Task RaritiesCanBeMovedThroughTheApi()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreateCatalogEntryAsync(client, token, RaritiesPath, "Common");
        var rareId = await CreateCatalogEntryAsync(client, token, RaritiesPath, "Rare");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Post,
            $"{RaritiesPath}/{rareId:D}/move-up", token);

        Assert.Equal(0, Property(await JsonAsync(response), "position").GetInt32());
    }

    [Fact]
    public async Task ReaderCannotPrint()
    {
        using var client = factory.CreateClient();
        var (token, itemId) = await ReaderTokenAfterItemAsync(client);

        using var response = await PdfRequestAsync(client, token, "print", new { printId = Guid.NewGuid(), itemIds = new[] { itemId }, copiesPerItem = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCanPreviewCardsAsPdf()
    {
        using var client = factory.CreateClient();
        var (token, itemId) = await ReaderTokenAfterItemAsync(client);

        using var response = await PdfRequestAsync(client, token, "preview", new { itemIds = new[] { itemId }, copiesPerItem = 1 });

        Assert.Equal((HttpStatusCode.OK, "application/pdf"), (response.StatusCode, response.Content.Headers.ContentType?.MediaType));
    }

    [Fact]
    public async Task PrintedCardShowsTheFrontAndTheBack()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var itemId = await ItemIdAsync(client, token, "Medkit");

        using var response = await PdfRequestAsync(client, token, "print", new { printId = Guid.NewGuid(), itemIds = new[] { itemId }, copiesPerItem = 1 });

        Assert.Empty(FrontAndBackWords.Except(await PdfWordsAsync(response)));
    }

    [Fact]
    public async Task PrintedPdfHasOnePagePerFourCards()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var itemId = await ItemIdAsync(client, token, "Medkit");

        using var response = await PdfRequestAsync(client, token, "print", new { printId = Guid.NewGuid(), itemIds = new[] { itemId }, copiesPerItem = 5 });

        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(2, pdf.NumberOfPages);
    }

    [Fact]
    public async Task UnknownItemsAreListedInTheError()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var unknown = Guid.NewGuid();

        using var response = await PdfRequestAsync(client, token, "preview", new { itemIds = new[] { unknown }, copiesPerItem = 1 });

        Assert.Equal([unknown.ToString("D")], (await JsonAsync(response)).GetProperty("properties").GetProperty("errors")
            .GetProperty("itemIds").EnumerateArray().Select(id => id.GetString()));
    }

    [Fact]
    public async Task CopiesCanBeDeactivatedThroughTheApi()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var itemId = await ItemIdAsync(client, token, "Medkit");
        (await PdfRequestAsync(client, token, "print", new { printId = Guid.NewGuid(), itemIds = new[] { itemId }, copiesPerItem = 1 })).Dispose();
        using var copies = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{ItemsPath}/{itemId:D}/copies", token);
        var deactivate = (await JsonAsync(copies)).GetProperty("entities")[0].GetProperty("actions")[0].GetProperty("href").GetString()!;

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Post, deactivate, token);

        Assert.False(Property(await JsonAsync(response), "isActive").GetBoolean());
    }

    [Fact]
    public async Task WholePrintIsReprintedAsPdf()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var itemId = await ItemIdAsync(client, token, "Medkit");
        var printId = Guid.NewGuid();
        (await PdfRequestAsync(client, token, "print", new { printId, itemIds = new[] { itemId }, copiesPerItem = 1 })).Dispose();

        using var response = await PdfRequestAsync(client, token, "reprint-print", new { printId });

        Assert.Equal((HttpStatusCode.OK, "application/pdf"), (response.StatusCode, response.Content.Headers.ContentType?.MediaType));
    }

    [Fact]
    public async Task CopySelfLinkReturnsTheCopy()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var itemId = await ItemIdAsync(client, token, "Medkit");
        (await PdfRequestAsync(client, token, "print", new { printId = Guid.NewGuid(), itemIds = new[] { itemId }, copiesPerItem = 1 })).Dispose();
        using var copies = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{ItemsPath}/{itemId:D}/copies", token);
        var listed = (await JsonAsync(copies)).GetProperty("entities")[0];

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get,
            listed.GetProperty("href").GetString()!, token);
        var copy = await JsonAsync(response);

        Assert.Equal((HttpStatusCode.OK, listed.GetProperty("properties").GetProperty("qrCode").GetString(), listed.GetProperty("href").GetString()),
            (response.StatusCode, Property(copy, "qrCode").GetString(), Hrefs(copy, "links").First()));
    }

    [Fact]
    public async Task UnknownCopyIsNotFound()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get,
            $"/local/api/v1/item-copies/{Guid.NewGuid():D}", token);

        Assert.Equal((HttpStatusCode.NotFound, "errors.notFound"), await ErrorAsync(response));
    }

    [Theory]
    [InlineData("POST", "item-cards/reprint")]
    [InlineData("POST", "item-cards/reprint-print")]
    [InlineData("POST", "item-copies/{copy}/deactivate")]
    [InlineData("POST", "item-copies/{copy}/reactivate")]
    [InlineData("POST", "items/{item}/components")]
    [InlineData("PUT", "items/{item}/components/{copy}")]
    [InlineData("DELETE", "items/{item}/components/{copy}")]
    [InlineData("PUT", "items/{item}/image")]
    [InlineData("DELETE", "items/{item}/image")]
    [InlineData("POST", "items/{item}/archive")]
    [InlineData("PUT", "items/{item}")]
    public async Task ReaderCannotChangeItemsOrCopies(string method, string path)
    {
        using var client = factory.CreateClient();
        var (token, itemId) = await ReaderTokenAfterItemAsync(client);
        var href = "/local/api/v1/" + path.Replace("{item}", itemId.ToString("D"), StringComparison.Ordinal)
            .Replace("{copy}", Guid.NewGuid().ToString("D"), StringComparison.Ordinal);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, new HttpMethod(method), href, token,
            new { version = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ItemImageIsServedWithItsTypeAndNoSniffing()
    {
        using var client = factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var itemId = await ItemIdAsync(client, token, "Medkit");
        using var set = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Put,
            $"{ItemsPath}/{itemId:D}/image", token, new { version = 1, content = ItemTestImages.Png() });
        var imageHref = Hrefs(await JsonAsync(set), "links").Single(href => href!.Contains("item-images", StringComparison.Ordinal))!;

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, imageHref, token);

        Assert.Equal(("image/png", "nosniff"),
            (response.Content.Headers.ContentType?.MediaType, string.Join(',', response.Headers.GetValues("X-Content-Type-Options"))));
    }

    private async Task<string> AdministratorTokenAsync(HttpClient client)
    {
        await factory.BootstrapAdministratorAsync();
        return await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1");
    }

    /// <summary>Creates the reader, then an item as the administrator, and signs in as the reader.</summary>
    private async Task<(string Token, Guid ItemId)> ReaderTokenAfterItemAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        var itemId = await ItemIdAsync(client, await AdministratorAuthenticationFactory.SignInAsync(client, "local-admin-password-1"), "Medkit");
        return (await AdministratorAuthenticationFactory.SignInReaderAsync(client), itemId);
    }

    private async Task<string> ReaderTokenAsync(HttpClient client)
    {
        await factory.CreateReaderAccountAsync(client);
        return await AdministratorAuthenticationFactory.SignInReaderAsync(client);
    }

    private static async Task<Guid> CreateCatalogEntryAsync(HttpClient client, string token, string path, string name)
    {
        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, path, token, new { name });
        response.EnsureSuccessStatusCode();
        var properties = (await JsonAsync(response)).GetProperty("properties");
        return (properties.TryGetProperty("itemTypeId", out var id) ? id : properties.GetProperty("rarityId")).GetGuid();
    }

    private static async Task<HttpResponseMessage> CreateItemAsync(HttpClient client, string token, string name,
        object[]? components = null, int cost = 25)
    {
        var typeId = await CreateCatalogEntryAsync(client, token, ItemTypesPath, $"Type {name}");
        var rarityId = await CreateCatalogEntryAsync(client, token, RaritiesPath, $"Rarity {name}");
        return await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, ItemsPath, token, new
        {
            name,
            itemTypeId = typeId,
            rarityId,
            description = "A small kit.",
            effect = "Heals two wounds.",
            cost,
            charges = 3,
            expiresOn = "2027-05-01",
            components
        });
    }

    private static async Task<Guid> ItemIdAsync(HttpClient client, string token, string name, object[]? components = null)
    {
        using var response = await CreateItemAsync(client, token, name, components);
        response.EnsureSuccessStatusCode();
        return Property(await JsonAsync(response), "itemId").GetGuid();
    }

    private static async Task<HttpResponseMessage> PdfRequestAsync(HttpClient client, string token, string action, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{CardsPath}/{action}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/pdf");
        request.Headers.Accept.ParseAdd(SirenMediaTypes.Json);
        return await client.SendAsync(request, Token);
    }

    private static async Task<string[]> PdfWordsAsync(HttpResponseMessage response)
    {
        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(Token));
        return [.. pdf.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text)];
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement.Clone();

    private static JsonElement Property(JsonElement entity, string name) => entity.GetProperty("properties").GetProperty(name);

    private static async Task<(HttpStatusCode, string?)> ErrorAsync(HttpResponseMessage response) =>
        (response.StatusCode, Property(await JsonAsync(response), "code").GetString());

    private static string?[] ActionNames(JsonElement entity) =>
        entity.TryGetProperty("actions", out var actions) ? [.. actions.EnumerateArray().Select(action => action.GetProperty("name").GetString())] : [];

    private static string?[] EntityNames(JsonElement entity) =>
        [.. entity.GetProperty("entities").EnumerateArray().Select(item => item.GetProperty("properties").GetProperty("name").GetString())];

    private static IEnumerable<string?> Hrefs(JsonElement entity, string member) =>
        entity.GetProperty(member).EnumerateArray().Select(link => link.GetProperty("href").GetString());
}
