using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

[Collection("Backend database integration")]
public sealed class CharacterServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private IPlayerService Players => factory.Services.GetRequiredService<IPlayerService>();

    private ICharacterService Characters => factory.Services.GetRequiredService<ICharacterService>();

    private INetworkService Networks => factory.Services.GetRequiredService<INetworkService>();

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreatedCharacterKeepsEveryField()
    {
        var player = await CreatePlayerAsync("Ann");
        var network = await CreateNetworkAsync("rim");

        var result = await Characters.CreateCharacterAsync(
            player.PlayerId,
            new CharacterDefinition(" Kira ", " Captain ", " Human ", " Female ", " Pilot ", int.MaxValue, CharacterStatus.Active,
                null, network.NetworkId, [], []),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new CharacterSummary(result.Value!.CharacterId, 1, player.PlayerId, "Ann", "Kira", "Captain", "Human", "Female", "Pilot",
                int.MaxValue, CharacterStatus.Active, null, network.NetworkId, result.Value.QrIdentifier, [], false, 1),
            result.Value with { Stats = [] });
    }

    [Fact]
    public async Task NewCharactersGetUniqueThirtyTwoDigitQrIdentifiers()
    {
        var player = await CreatePlayerAsync("Ann");
        var first = await CreateCharacterAsync(player, "Kira");
        var second = await CreateCharacterAsync(player, "Vel");

        Assert.Equal(
            new { FirstValid = true, SecondValid = true, Different = true },
            new
            {
                FirstValid = IsStoredIdentifier(first.QrIdentifier),
                SecondValid = IsStoredIdentifier(second.QrIdentifier),
                Different = first.QrIdentifier != second.QrIdentifier
            });
    }

    [Fact]
    public async Task InvalidNewCharacterIsRejectedAndNotStored()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named(" "),
            TestContext.Current.CancellationToken);

        Assert.Equal(new { Outcome = RosterOutcome.InvalidName, StoredCount = 0 },
            new { result.Outcome, StoredCount = (await StoredCharactersOfAsync(player)).Count });
    }

    [Fact]
    public async Task InvalidNewStatIsRejectedAndNothingIsStored()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NewStats = [new NewCharacterStat("Rank", (CharacterStatType)9, 1, null)] },
            TestContext.Current.CancellationToken);

        var stats = await factory.Services.GetRequiredService<ICharacterStatService>()
            .GetStatsAsync(includeArchived: true, TestContext.Current.CancellationToken);
        Assert.Equal(new { Outcome = RosterOutcome.InvalidStatType, StoredCharacters = 0, StoredStats = 0 },
            new { result.Outcome, StoredCharacters = (await StoredCharactersOfAsync(player)).Count, StoredStats = stats.Count });
    }

    [Fact]
    public async Task ValueForAnUnknownStatIsRejected()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { StatValues = [new CharacterStatInput(Guid.NewGuid(), 1, null)] },
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.UnknownStat, result.Outcome);
    }

    [Fact]
    public async Task CharacterOfAnUnknownPlayerIsNotFound()
    {
        var result = await Characters.CreateCharacterAsync(Guid.NewGuid(), CharacterDefinitions.Named("Kira"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task InactivePlayerCannotGetNewCharacters()
    {
        var player = await CreatePlayerAsync("Ann");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        var result = await Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named("Kira"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.PlayerInactive, result.Outcome);
    }

    [Fact]
    public async Task UnknownNetworkIsRejected()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NetworkId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.UnknownNetwork, result.Outcome);
    }

    [Fact]
    public async Task DisabledNetworkCannotBeChosen()
    {
        var player = await CreatePlayerAsync("Ann");
        var network = await CreateNetworkAsync("rim");
        await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NetworkId = network.NetworkId }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NetworkDisabled, result.Outcome);
    }

    [Fact]
    public async Task DefaultNetworkCannotBeChosen()
    {
        var player = await CreatePlayerAsync("Ann");
        var network = await CreateNetworkAsync("core");
        await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NetworkId = network.NetworkId }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.DefaultNetworkNotSelectable, result.Outcome);
    }

    [Fact]
    public async Task NetworkThatLaterBecameTheDefaultIsKeptByOtherEdits()
    {
        var player = await CreatePlayerAsync("Ann");
        var network = await CreateNetworkAsync("rim");
        var character = await CreateCharacterAsync(player, "Kira", network.NetworkId);
        await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Title = "Captain" }, TestContext.Current.CancellationToken);

        Assert.Equal(network.NetworkId, result.Value!.NetworkId);
    }

    [Fact]
    public async Task NetworkCanBeRemoved()
    {
        var player = await CreatePlayerAsync("Ann");
        var network = await CreateNetworkAsync("rim");
        var character = await CreateCharacterAsync(player, "Kira", network.NetworkId);

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { NetworkId = null }, TestContext.Current.CancellationToken);

        Assert.Null(result.Value!.NetworkId);
    }

    [Fact]
    public async Task DeadCharacterKeepsItsDeathDate()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Dead, DeathDate = new DateOnly(2026, 5, 1) },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Status = CharacterStatus.Dead, DeathDate = (DateOnly?)new DateOnly(2026, 5, 1) },
            new { result.Value!.Status, result.Value.DeathDate });
    }

    [Fact]
    public async Task DeathDateIsClearedWhenTheCharacterIsNoLongerDead()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");
        var dead = (await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Dead, DeathDate = new DateOnly(2026, 5, 1) },
            TestContext.Current.CancellationToken)).Value!;

        var result = await Characters.UpdateCharacterAsync(dead.CharacterId, dead.Version,
            CharacterDefinitions.From(dead) with { Status = CharacterStatus.Retired }, TestContext.Current.CancellationToken);

        Assert.Null(result.Value!.DeathDate);
    }

    [Fact]
    public async Task InvalidEditIsRejectedAndChangesNothing()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Dead }, TestContext.Current.CancellationToken);

        var stored = await Characters.GetCharacterAsync(character.CharacterId, TestContext.Current.CancellationToken);
        Assert.Equal(new { Outcome = RosterOutcome.DeathDateRequired, Stored = character with { Stats = [] } },
            new { result.Outcome, Stored = stored! with { Stats = [] } });
    }

    [Fact]
    public async Task CharacterOfAnInactivePlayerCannotBecomeActive()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);
        var retired = (await Characters.GetCharactersOfPlayerAsync(player.PlayerId, false, TestContext.Current.CancellationToken))[0];

        var result = await Characters.UpdateCharacterAsync(retired.CharacterId, retired.Version,
            CharacterDefinitions.From(retired) with { Status = CharacterStatus.Active }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.PlayerInactive, result.Outcome);
    }

    [Fact]
    public async Task StaleEditIsRejected()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");
        await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Title = "Captain" }, TestContext.Current.CancellationToken);

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Title = "Major" }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task UnchangedEditKeepsTheVersion()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character), TestContext.Current.CancellationToken);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task ActiveCharacterCannotBeArchived()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");

        var result = await Characters.ArchiveCharacterAsync(character.CharacterId, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.CharacterNotArchivable, result.Outcome);
    }

    [Fact]
    public async Task RetiredCharacterCanBeArchivedAndIsThenHidden()
    {
        var player = await CreatePlayerAsync("Ann");
        var retired = await RetireAsync(await CreateCharacterAsync(player, "Kira"));

        await Characters.ArchiveCharacterAsync(retired.CharacterId, TestContext.Current.CancellationToken);

        Assert.Empty(await Characters.GetCharactersOfPlayerAsync(player.PlayerId, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ArchivedCharacterCannotBecomeActive()
    {
        var retired = await RetireAsync(await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira"));
        var archived = (await Characters.ArchiveCharacterAsync(retired.CharacterId, TestContext.Current.CancellationToken)).Value!;

        var result = await Characters.UpdateCharacterAsync(archived.CharacterId, archived.Version,
            CharacterDefinitions.From(archived) with { Status = CharacterStatus.Active }, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.Archived, result.Outcome);
    }

    [Fact]
    public async Task UnarchivedCharacterIsListedAgain()
    {
        var player = await CreatePlayerAsync("Ann");
        var retired = await RetireAsync(await CreateCharacterAsync(player, "Kira"));
        await Characters.ArchiveCharacterAsync(retired.CharacterId, TestContext.Current.CancellationToken);

        await Characters.UnarchiveCharacterAsync(retired.CharacterId, TestContext.Current.CancellationToken);

        Assert.Single(await Characters.GetCharactersOfPlayerAsync(player.PlayerId, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RegeneratedQrIdentifierReplacesTheOldOne()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");
        var regenerated = (await Characters.RegenerateQrIdentifierAsync(character.CharacterId, TestContext.Current.CancellationToken)).Value!;

        var oldLookup = await Characters.FindByQrIdentifierAsync(character.QrIdentifier, TestContext.Current.CancellationToken);
        var newLookup = await Characters.FindByQrIdentifierAsync(regenerated.QrIdentifier, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Old = RosterOutcome.NotFound, New = (Guid?)character.CharacterId },
            new { Old = oldLookup.Outcome, New = newLookup.Value?.CharacterId });
    }

    [Fact]
    public async Task QrLookupAcceptsTheHyphenatedForm()
    {
        var character = await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira");

        var result = await Characters.FindByQrIdentifierAsync(
            Guid.ParseExact(character.QrIdentifier, "N").ToString("D").ToUpperInvariant(), TestContext.Current.CancellationToken);

        Assert.Equal(character.CharacterId, result.Value!.CharacterId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-code")]
    public async Task MalformedQrIdentifierIsInvalid(string identifier)
    {
        var result = await Characters.FindByQrIdentifierAsync(identifier, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.InvalidQrIdentifier, result.Outcome);
    }

    [Fact]
    public async Task NewStatGivesItsFirstValueToEveryExistingCharacter()
    {
        var player = await CreatePlayerAsync("Ann");
        var other = await CreateCharacterAsync(player, "Vel");
        await CreateCharacterAsync(player, "Kira", newStats:
        [
            new NewCharacterStat("Dark Side", CharacterStatType.Number, 5, null),
            new NewCharacterStat("Teaching", CharacterStatType.Text, null, "adept")
        ]);

        var reloaded = await Characters.GetCharacterAsync(other.CharacterId, TestContext.Current.CancellationToken);

        Assert.Equal("Dark Side=5, Teaching=adept", StatText(reloaded!));
    }

    [Fact]
    public async Task CharacterCreatedLaterStartsWithTheStatsDefault()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira", newStats: [new NewCharacterStat("Dark Side", CharacterStatType.Number, 5, null)]);

        var later = await CreateCharacterAsync(player, "Vel");

        Assert.Equal("Dark Side=5", StatText(later));
    }

    [Fact]
    public async Task OwnValueReplacesTheDefaultOnlyForThatCharacter()
    {
        var player = await CreatePlayerAsync("Ann");
        var kira = await CreateCharacterAsync(player, "Kira", newStats: [new NewCharacterStat("Dark Side", CharacterStatType.Number, 5, null)]);
        var vel = await CreateCharacterAsync(player, "Vel");
        await Characters.UpdateCharacterAsync(kira.CharacterId, kira.Version,
            CharacterDefinitions.From(kira) with { StatValues = [new CharacterStatInput(kira.Stats[0].StatId, 2, null)] },
            TestContext.Current.CancellationToken);

        var reloaded = await Characters.GetCharacterAsync(vel.CharacterId, TestContext.Current.CancellationToken);

        Assert.Equal("Dark Side=5", StatText(reloaded!));
    }

    [Fact]
    public async Task TypingAnExistingStatNameDoesNotChangeItsDefault()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira", newStats: [new NewCharacterStat("Dark Side", CharacterStatType.Number, 5, null)]);
        await CreateCharacterAsync(player, "Vel", newStats: [new NewCharacterStat("dark side", CharacterStatType.Number, 1, null)]);

        var later = await CreateCharacterAsync(player, "Cai");

        Assert.Equal("Dark Side=5", StatText(later));
    }

    [Fact]
    public async Task ClearedTextStatStaysEmptyInsteadOfShowingTheDefault()
    {
        var player = await CreatePlayerAsync("Ann");
        var kira = await CreateCharacterAsync(player, "Kira", newStats: [new NewCharacterStat("Teaching", CharacterStatType.Text, null, "adept")]);

        var result = await Characters.UpdateCharacterAsync(kira.CharacterId, kira.Version,
            CharacterDefinitions.From(kira) with { StatValues = [new CharacterStatInput(kira.Stats[0].StatId, null, " ")] },
            TestContext.Current.CancellationToken);

        Assert.Equal("Teaching=", StatText(result.Value!));
    }

    [Fact]
    public async Task SavingTheDefaultValueChangesNothing()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira", newStats: [new NewCharacterStat("Dark Side", CharacterStatType.Number, 5, null)]);
        var vel = await CreateCharacterAsync(player, "Vel");

        var result = await Characters.UpdateCharacterAsync(vel.CharacterId, vel.Version,
            CharacterDefinitions.From(vel) with { StatValues = [new CharacterStatInput(vel.Stats[0].StatId, 5, null)] },
            TestContext.Current.CancellationToken);

        Assert.Equal(vel.Version, result.Value!.Version);
    }

    [Fact]
    public async Task TypedStatNameThatExistsWithTheSameTypeUsesThatStat()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Vel", newStats: [new NewCharacterStat("Rank", CharacterStatType.Number, 1, null)]);

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NewStats = [new NewCharacterStat(" RANK ", CharacterStatType.Number, 4, null)] },
            TestContext.Current.CancellationToken);

        Assert.Equal("Rank=4", string.Join(", ", result.Value!.Stats.Select(stat => $"{stat.Name}={stat.Number}")));
    }

    [Fact]
    public async Task TypedStatNameThatExistsWithAnotherTypeIsRejected()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Vel", newStats: [new NewCharacterStat("Rank", CharacterStatType.Number, 1, null)]);

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NewStats = [new NewCharacterStat("Rank", CharacterStatType.Text, null, "high")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task TypedStatNameOfAnArchivedStatIsRejected()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Vel", newStats: [new NewCharacterStat("Dark Side", CharacterStatType.Number, 5, null)]);
        var stats = factory.Services.GetRequiredService<ICharacterStatService>();
        var darkSide = (await stats.GetStatsAsync(includeArchived: false, TestContext.Current.CancellationToken)).Single();
        await stats.ArchiveStatAsync(darkSide.StatId, TestContext.Current.CancellationToken);

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { NewStats = [new NewCharacterStat("dark side", CharacterStatType.Number, 3, null)] },
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.StatArchived, result.Outcome);
    }

    [Fact]
    public async Task ValueOfTheWrongTypeForAnExistingStatIsRejected()
    {
        var player = await CreatePlayerAsync("Ann");
        var vel = await CreateCharacterAsync(player, "Vel", newStats: [new NewCharacterStat("Rank", CharacterStatType.Number, 1, null)]);

        var result = await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named("Kira") with { StatValues = [new CharacterStatInput(vel.Stats[0].StatId, null, "high")] },
            TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.InvalidStatValue, result.Outcome);
    }

    [Fact]
    public async Task EditKeepsTheValuesOfStatsItDoesNotList()
    {
        var player = await CreatePlayerAsync("Ann");
        var character = await CreateCharacterAsync(player, "Kira", newStats:
        [
            new NewCharacterStat("Dark Side", CharacterStatType.Number, 3, null),
            new NewCharacterStat("Teaching", CharacterStatType.Text, null, "adept")
        ]);
        var darkSide = character.Stats.Single(stat => stat.Name == "Dark Side").StatId;

        var result = await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { StatValues = [new CharacterStatInput(darkSide, 5, null)] },
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "Dark Side=5, Teaching=adept",
            string.Join(", ", result.Value!.Stats.Select(stat => $"{stat.Name}={stat.Number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? stat.Text}")));
    }

    [Fact]
    public async Task SearchFindsCharactersOfEveryPlayerByName()
    {
        await CreateCharacterAsync(await CreatePlayerAsync("Ann"), "Kira Nerys");
        await CreateCharacterAsync(await CreatePlayerAsync("Bob"), "Vel");

        var page = await Characters.SearchCharactersAsync(new RosterQuery("nerys", false, 1), TestContext.Current.CancellationToken);

        Assert.Equal("Kira Nerys (Ann)", string.Join(", ", page.Items.Select(character => $"{character.Name} ({character.PlayerDisplayName})")));
    }

    [Fact]
    public async Task SearchFindsCharactersByRunningNumber()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");
        await CreateCharacterAsync(player, "Vel");

        var page = await Characters.SearchCharactersAsync(new RosterQuery(" 2 ", false, 1), TestContext.Current.CancellationToken);

        Assert.Equal("Vel", string.Join(", ", page.Items.Select(character => character.Name)));
    }

    [Fact]
    public async Task OverlappingEditsDefiningTheSameStatNameLetOnlyOneSucceed()
    {
        var player = await CreatePlayerAsync("Ann");
        var kira = await CreateCharacterAsync(player, "Kira");
        var vel = await CreateCharacterAsync(player, "Vel");
        using var overlap = new Barrier(2);
        factory.Clock.ReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Characters.UpdateCharacterAsync(kira.CharacterId, kira.Version,
                CharacterDefinitions.From(kira) with { NewStats = [new NewCharacterStat("Rank", CharacterStatType.Number, 1, null)] },
                TestContext.Current.CancellationToken)),
            Task.Run(() => Characters.UpdateCharacterAsync(vel.CharacterId, vel.Version,
                CharacterDefinitions.From(vel) with { NewStats = [new NewCharacterStat("RANK", CharacterStatType.Number, 2, null)] },
                TestContext.Current.CancellationToken)));
        factory.Clock.ReadBarrier = null;

        Assert.Equal([RosterOutcome.Succeeded, RosterOutcome.NameAlreadyUsed], results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task OverlappingCreationsBothSucceedWithTheirOwnRunningNumbers()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");
        using var overlap = new Barrier(2);
        factory.Clock.FirstReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named("Vel"),
                TestContext.Current.CancellationToken)),
            Task.Run(() => Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named("Zed"),
                TestContext.Current.CancellationToken)));

        Assert.Equal([2, 3], results.Select(result => result.Value?.Number).Order());
    }

    private static string StatText(CharacterSummary character) =>
        string.Join(", ", character.Stats.Select(stat =>
            $"{stat.Name}={stat.Number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? stat.Text}"));

    private static bool IsStoredIdentifier(string identifier) =>
        identifier.Length == 32 && identifier.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private async Task<PlayerSummary> CreatePlayerAsync(string name) =>
        (await Players.CreatePlayerAsync(new PlayerDefinition(name, null, null, null, PlayerType.Player),
            TestContext.Current.CancellationToken)).Value!;

    private Task<IReadOnlyList<CharacterSummary>> StoredCharactersOfAsync(PlayerSummary player) =>
        Characters.GetCharactersOfPlayerAsync(player.PlayerId, includeArchived: true, TestContext.Current.CancellationToken);

    private async Task<NetworkSummary> CreateNetworkAsync(string code) =>
        (await Networks.CreateNetworkAsync(code, new NetworkDefinition(code, null, NetworkTheme.Blue),
            TestContext.Current.CancellationToken)).Value!;

    private async Task<CharacterSummary> CreateCharacterAsync(
        PlayerSummary player,
        string name,
        Guid? networkId = null,
        IReadOnlyList<NewCharacterStat>? newStats = null) =>
        (await Characters.CreateCharacterAsync(
            player.PlayerId,
            CharacterDefinitions.Named(name) with { NetworkId = networkId, NewStats = newStats ?? [] },
            TestContext.Current.CancellationToken)).Value!;

    private async Task<CharacterSummary> RetireAsync(CharacterSummary character) =>
        (await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Retired },
            TestContext.Current.CancellationToken)).Value!;
}
