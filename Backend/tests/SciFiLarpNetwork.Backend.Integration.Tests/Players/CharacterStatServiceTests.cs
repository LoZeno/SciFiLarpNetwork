using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

[Collection("Backend database integration")]
public sealed class CharacterStatServiceTests : IAsyncLifetime
{
    private readonly AdministratorAuthenticationFactory _factory = new();

    private IPlayerService Players => _factory.Services.GetRequiredService<IPlayerService>();

    private ICharacterService Characters => _factory.Services.GetRequiredService<ICharacterService>();

    private ICharacterStatService Stats => _factory.Services.GetRequiredService<ICharacterStatService>();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task RenameKeepsEveryCharactersValue()
    {
        var character = await CreateCharacterWithStatAsync("Dark Side", 3);
        var stat = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken))[0];

        await Stats.RenameStatAsync(stat.StatId, stat.Version, "Shadow", TestContext.Current.CancellationToken);

        var reloaded = await Characters.GetCharacterAsync(character.CharacterId, TestContext.Current.CancellationToken);
        Assert.Equal("Shadow=3", string.Join(", ", reloaded!.Stats.Select(value => $"{value.Name}={value.Number}")));
    }

    [Fact]
    public async Task RenameCannotTakeAnotherStatsName()
    {
        await CreateCharacterWithStatAsync("Dark Side", 3);
        await CreateCharacterWithStatAsync("Rank", 1);
        var rank = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken)).Single(stat => stat.Name == "Rank");

        var result = await Stats.RenameStatAsync(rank.StatId, rank.Version, "DARK SIDE", TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task BlankRenameIsRejected()
    {
        await CreateCharacterWithStatAsync("Rank", 1);
        var rank = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken))[0];

        var result = await Stats.RenameStatAsync(rank.StatId, rank.Version, " ", TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.InvalidStatName, result.Outcome);
    }

    [Fact]
    public async Task StaleRenameIsRejected()
    {
        await CreateCharacterWithStatAsync("Rank", 1);
        var rank = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken))[0];
        await Stats.RenameStatAsync(rank.StatId, rank.Version, "Grade", TestContext.Current.CancellationToken);

        var result = await Stats.RenameStatAsync(rank.StatId, rank.Version, "Level", TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task ArchivedStatIsHiddenUnlessRequested()
    {
        await CreateCharacterWithStatAsync("Rank", 1);
        var rank = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken))[0];

        await Stats.ArchiveStatAsync(rank.StatId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Default = 0, All = 1 },
            new
            {
                Default = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken)).Count,
                All = (await Stats.GetStatsAsync(true, TestContext.Current.CancellationToken)).Count
            });
    }

    [Fact]
    public async Task ArchivedStatKeepsCharacterValuesAndIsMarked()
    {
        var character = await CreateCharacterWithStatAsync("Rank", 4);
        var rank = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken))[0];

        await Stats.ArchiveStatAsync(rank.StatId, TestContext.Current.CancellationToken);

        var reloaded = await Characters.GetCharacterAsync(character.CharacterId, TestContext.Current.CancellationToken);
        Assert.Equal(
            new CharacterStatValueSummary(rank.StatId, "Rank", CharacterStatType.Number, 4, null, true),
            reloaded!.Stats.Single());
    }

    [Fact]
    public async Task UnarchivedStatIsListedAgain()
    {
        await CreateCharacterWithStatAsync("Rank", 1);
        var rank = (await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken))[0];
        await Stats.ArchiveStatAsync(rank.StatId, TestContext.Current.CancellationToken);

        await Stats.UnarchiveStatAsync(rank.StatId, TestContext.Current.CancellationToken);

        Assert.Single(await Stats.GetStatsAsync(false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ArchivingAnUnknownStatIsNotFound()
    {
        var result = await Stats.ArchiveStatAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NotFound, result.Outcome);
    }

    private async Task<CharacterSummary> CreateCharacterWithStatAsync(string statName, int value)
    {
        var player = (await Players.CreatePlayerAsync(new PlayerDefinition("Ann", null, null, null, PlayerType.Player),
            TestContext.Current.CancellationToken)).Value!;
        return (await Characters.CreateCharacterAsync(
            player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NewStats = [new NewCharacterStat(statName, CharacterStatType.Number, value, null)] },
            TestContext.Current.CancellationToken)).Value!;
    }
}
