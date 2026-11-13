using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Skills;

[Collection("Backend database integration")]
public sealed class TalentServiceTests : IAsyncLifetime
{
    private readonly AdministratorAuthenticationFactory _factory = new();

    private ITalentService Talents => _factory.Services.GetRequiredService<ITalentService>();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreatedTalentStartsAtVersionOne()
    {
        var result = await Talents.CreateTalentAsync(new TalentDefinition("  Medicine  ", "Healing arts"), TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Outcome = CatalogOutcome.Succeeded, Name = "Medicine", Description = (string?)"Healing arts", IsArchived = false, Version = 1L },
            new { result.Outcome, result.Value!.Name, result.Value.Description, result.Value.IsArchived, result.Value.Version });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankTalentNameIsRejected(string name)
    {
        var result = await Talents.CreateTalentAsync(new TalentDefinition(name, null), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.InvalidName, result.Outcome);
    }

    [Fact]
    public async Task DuplicateTalentNameIsRejectedIgnoringCase()
    {
        await CreateAsync("Medicine");

        var result = await Talents.CreateTalentAsync(new TalentDefinition("MEDICINE", null), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task UpdatedTalentHasNewDetailsAndVersion()
    {
        var talent = await CreateAsync("Medicine");

        var result = await Talents.UpdateTalentAsync(talent.TalentId, talent.Version, new TalentDefinition("Surgery", null), TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Name = "Surgery", Version = 2L },
            new { result.Value!.Name, result.Value.Version });
    }

    [Fact]
    public async Task UpdateFromAStaleVersionIsRejected()
    {
        var talent = await CreateAsync("Medicine");
        await Talents.UpdateTalentAsync(talent.TalentId, talent.Version, new TalentDefinition("Surgery", null), TestContext.Current.CancellationToken);

        var result = await Talents.UpdateTalentAsync(talent.TalentId, talent.Version, new TalentDefinition("Field medicine", null), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task RenamingToAnotherTalentsNameIsRejected()
    {
        await CreateAsync("Medicine");
        var talent = await CreateAsync("Engineering");

        var result = await Talents.UpdateTalentAsync(talent.TalentId, talent.Version, new TalentDefinition("medicine", null), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task UpdatingAnUnknownTalentReportsNotFound()
    {
        var result = await Talents.UpdateTalentAsync(Guid.NewGuid(), 1, new TalentDefinition("Medicine", null), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task ArchivedTalentIsHiddenFromTheDefaultList()
    {
        var talent = await CreateAsync("Medicine");
        await CreateAsync("Engineering");

        await Talents.ArchiveTalentAsync(talent.TalentId, TestContext.Current.CancellationToken);

        Assert.Equal(["Engineering"], (await Talents.GetTalentsAsync(includeArchived: false, TestContext.Current.CancellationToken)).Select(t => t.Name));
    }

    [Fact]
    public async Task ArchivedTalentIsListedWhenRequested()
    {
        var talent = await CreateAsync("Medicine");

        await Talents.ArchiveTalentAsync(talent.TalentId, TestContext.Current.CancellationToken);

        Assert.True((await Talents.GetTalentsAsync(includeArchived: true, TestContext.Current.CancellationToken)).Single().IsArchived);
    }

    [Fact]
    public async Task UnarchivedTalentIsActiveAgain()
    {
        var talent = await CreateAsync("Medicine");
        await Talents.ArchiveTalentAsync(talent.TalentId, TestContext.Current.CancellationToken);

        var result = await Talents.UnarchiveTalentAsync(talent.TalentId, TestContext.Current.CancellationToken);

        Assert.False(result.Value!.IsArchived);
    }

    [Fact]
    public async Task ArchivingAnUnknownTalentReportsNotFound()
    {
        var result = await Talents.ArchiveTalentAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(CatalogOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task TalentsAreListedByName()
    {
        await CreateAsync("Medicine");
        await CreateAsync("athletics");
        await CreateAsync("Engineering");

        var names = (await Talents.GetTalentsAsync(includeArchived: false, TestContext.Current.CancellationToken)).Select(t => t.Name);

        Assert.Equal(["athletics", "Engineering", "Medicine"], names);
    }

    private async Task<TalentSummary> CreateAsync(string name) =>
        (await Talents.CreateTalentAsync(new TalentDefinition(name, null), TestContext.Current.CancellationToken)).Value!;
}
