using System.Net;
using System.Text.Json;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Skills;

[Collection("Backend database integration")]
public sealed class SkillCatalogApiTests : IAsyncLifetime
{
    private const string TalentsPath = "/local/api/v1/talents";
    private const string SkillsPath = "/local/api/v1/skills";
    private static readonly string[] HackingFlags = ["improvesHacking", "accessHacking"];
    private static readonly string[] ToughnessFlag = ["Toughness"];
    private readonly AdministratorAuthenticationFactory _factory = new();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task VersionRootAdvertisesTalentsToReaders()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1", token);

        Assert.Contains(TalentsPath, (await ReadAsync(response)).Links());
    }

    [Fact]
    public async Task AnonymousRequestCannotListTalents()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(TalentsPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReaderCanListTalentsWithoutWriteActions()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TalentsPath, token);

        Assert.Equal(
            new { Status = HttpStatusCode.OK, Actions = "" },
            new { Status = response.StatusCode, Actions = string.Join(',', (await ReadAsync(response)).ActionNames()) });
    }

    [Fact]
    public async Task AdministratorSeesTheCreateTalentAction()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TalentsPath, token);

        Assert.Equal(["create-talent"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task ReaderCannotCreateTalents()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, TalentsPath, token, new { name = "Medicine" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorCreatesATalent()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, TalentsPath, token, new { name = "Medicine", description = "Healing arts" });
        var talent = await ReadAsync(response);

        Assert.Equal(
            new { Status = HttpStatusCode.Created, Name = (string?)"Medicine", Version = 1L },
            new { Status = response.StatusCode, Name = talent.Property("name").GetString(), Version = talent.Property("version").GetInt64() });
    }

    [Fact]
    public async Task DuplicateTalentNameReturnsAStableConflictCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreateTalentAsync(client, token, "Medicine");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, TalentsPath, token, new { name = "medicine" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.nameAlreadyUsed"), await ErrorAsync(response));
    }

    [Fact]
    public async Task StaleTalentEditReturnsTheStaleVersionCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var talentId = await CreateTalentAsync(client, token, "Medicine");
        (await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{TalentsPath}/{talentId:D}", token, new { version = 1, name = "Surgery" })).Dispose();

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{TalentsPath}/{talentId:D}", token, new { version = 1, name = "Field medicine" });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.Conflict, "errors.staleVersion"), await ErrorAsync(response));
    }

    [Fact]
    public async Task UnknownTalentReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{TalentsPath}/{Guid.NewGuid():D}/skills", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ArchivedTalentAdvertisesRestoreInsteadOfArchive()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var talentId = await CreateTalentAsync(client, token, "Medicine");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, $"{TalentsPath}/{talentId:D}/archive", token, new { });

        Assert.Equal(["update-talent", "unarchive-talent"], (await ReadAsync(response)).ActionNames());
    }

    [Fact]
    public async Task TalentSkillsListsOnlyAssignedSkills()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var talentId = await CreateTalentAsync(client, token, "Medicine");
        await CreateSkillAsync(client, token, new { name = "First aid", cost = 1, talentIds = new[] { talentId } });
        await CreateSkillAsync(client, token, new { name = "Welding", cost = 1 });

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{TalentsPath}/{talentId:D}/skills", token);

        Assert.Equal(["First aid"], (await ReadAsync(response)).EntityNames());
    }

    [Fact]
    public async Task SkillCollectionOffersEveryLocalizedGameFlag()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, SkillsPath, token, "it");

        Assert.Equal(
            new { Count = 12, FirstName = "accessHacking", FirstTitle = "Accesso all'hacking" },
            GameFlagSnapshot((await ReadAsync(response)).Property("gameFlags")));
    }

    [Fact]
    public async Task SkillGameFlagsAreExchangedByName()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, SkillsPath, token, new { name = "Hacking I", cost = 2, gameFlags = HackingFlags });

        Assert.Equal(
            "accessHacking,improvesHacking",
            string.Join(',', (await ReadAsync(response)).Property("gameFlags").EnumerateArray().Select(flag => flag.GetString())));
    }

    [Fact]
    public async Task InvalidCostReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, SkillsPath, token, new { name = "Hacking I", cost = 1000 });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.invalidCost"), await ErrorAsync(response));
    }

    [Fact]
    public async Task PrerequisiteLoopReturnsItsStableCode()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var first = await CreateSkillAsync(client, token, new { name = "Hacking I", cost = 1 });
        await CreateSkillAsync(client, token, new { name = "Hacking II", cost = 1, prerequisiteGroups = new[] { new[] { first } } });
        var second = await FindSkillIdAsync(client, token, "Hacking II");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{SkillsPath}/{first:D}", token, new { version = 1, name = "Hacking I", cost = 1, prerequisiteGroups = new[] { new[] { second } } });

        Assert.Equal(new ErrorSnapshot(HttpStatusCode.BadRequest, "errors.prerequisiteLoop"), await ErrorAsync(response));
    }

    [Fact]
    public async Task SkillEditReturnsTheNextVersion()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var skillId = await CreateSkillAsync(client, token, new { name = "Welding", cost = 1 });

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Put, $"{SkillsPath}/{skillId:D}", token, new { version = 1, name = "Welding", description = "Joins metal", cost = 1 });

        Assert.Equal(2L, (await ReadAsync(response)).Property("version").GetInt64());
    }

    [Fact]
    public async Task ReaderCannotArchiveSkills()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _) = await _factory.PrepareReaderWithTokenAsync(client);
        var skillId = await CreateSkillAsync(client, adminToken, new { name = "Welding", cost = 1 });
        var readerToken = await AdministratorAuthenticationFactory.SignInReaderAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(
            client, HttpMethod.Post, $"{SkillsPath}/{skillId:D}/archive", readerToken, new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TypedCardFlagIsListedInTheCardFlagCatalog()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        await CreateSkillAsync(client, token, new { name = "Toughness I", cost = 1, newCardFlagNames = ToughnessFlag });

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, "/local/api/v1/skill-card-flags", token);

        Assert.Equal(
            ["Toughness"],
            (await ReadAsync(response)).Property("cardFlags").EnumerateArray().Select(flag => flag.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task TalentCollectionLinksToItsArchivedView()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TalentsPath, token);

        Assert.Equal($"{TalentsPath}?includeArchived=true", (await ReadAsync(response)).LinkHref(SirenRelations.WithArchived));
    }

    [Fact]
    public async Task ArchivedTalentViewLinksBackToTheActiveView()
    {
        using var client = _factory.CreateClient();
        var token = await ReaderTokenAsync(client);

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(
            client, HttpMethod.Get, $"{TalentsPath}?includeArchived=true", token);

        Assert.Equal(TalentsPath, (await ReadAsync(response)).LinkHref(SirenRelations.WithoutArchived));
    }

    [Fact]
    public async Task TalentItemsLinkToTheirSkills()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var talentId = await CreateTalentAsync(client, token, "Medicine");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, TalentsPath, token);

        Assert.Equal($"{TalentsPath}/{talentId:D}/skills", (await ReadAsync(response)).FirstEntityLinkHref(SirenRelations.Skills));
    }

    [Fact]
    public async Task TalentSkillsLinkToTheirArchivedView()
    {
        using var client = _factory.CreateClient();
        var token = await AdministratorTokenAsync(client);
        var talentId = await CreateTalentAsync(client, token, "Medicine");

        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, $"{TalentsPath}/{talentId:D}/skills", token);

        Assert.Equal(
            $"{TalentsPath}/{talentId:D}/skills?includeArchived=true",
            (await ReadAsync(response)).LinkHref(SirenRelations.WithArchived));
    }

    private static object GameFlagSnapshot(JsonElement gameFlags) =>
        new
        {
            Count = gameFlags.GetArrayLength(),
            FirstName = gameFlags[0].GetProperty("name").GetString(),
            FirstTitle = gameFlags[0].GetProperty("title").GetString()
        };

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

    private static async Task<Guid> CreateTalentAsync(HttpClient client, string token, string name)
    {
        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, TalentsPath, token, new { name });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).Property("talentId").GetGuid();
    }

    private static async Task<Guid> CreateSkillAsync(HttpClient client, string token, object skill)
    {
        using var response = await AdministratorAuthenticationFactory.SendAuthorizedJsonAsync(client, HttpMethod.Post, SkillsPath, token, skill);
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).Property("skillId").GetGuid();
    }

    private static async Task<Guid> FindSkillIdAsync(HttpClient client, string token, string name)
    {
        using var response = await AdministratorAuthenticationFactory.SendAuthorizedAsync(client, HttpMethod.Get, SkillsPath, token);
        var skills = await ReadAsync(response);
        return skills.Root.GetProperty("entities").EnumerateArray()
            .Single(entity => entity.GetProperty("properties").GetProperty("name").GetString() == name)
            .GetProperty("properties").GetProperty("skillId").GetGuid();
    }

    private static async Task<SirenDocument> ReadAsync(HttpResponseMessage response) =>
        new(JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone());

    private static async Task<ErrorSnapshot> ErrorAsync(HttpResponseMessage response) =>
        new(response.StatusCode, (await ReadAsync(response)).Property("code").GetString());

    private sealed record ErrorSnapshot(HttpStatusCode Status, string? Code);

    private sealed record SirenDocument(JsonElement Root)
    {
        public JsonElement Property(string name) => Root.GetProperty("properties").GetProperty(name);

        public IEnumerable<string?> Links() =>
            Root.GetProperty("links").EnumerateArray().Select(link => link.GetProperty("href").GetString());

        public string? LinkHref(string rel) => FindLinkHref(Root, rel);

        public string? FirstEntityLinkHref(string rel) => FindLinkHref(Root.GetProperty("entities")[0], rel);

        private static string? FindLinkHref(JsonElement entity, string rel) =>
            entity.GetProperty("links").EnumerateArray()
                .Single(link => link.GetProperty("rel").EnumerateArray().Any(value => value.GetString() == rel))
                .GetProperty("href").GetString();

        public string?[] ActionNames() =>
            Root.TryGetProperty("actions", out var actions)
                ? actions.EnumerateArray().Select(action => action.GetProperty("name").GetString()).ToArray()
                : [];

        public string?[] EntityNames() =>
            Root.GetProperty("entities").EnumerateArray()
                .Select(entity => entity.GetProperty("properties").GetProperty("name").GetString())
                .ToArray();
    }
}
