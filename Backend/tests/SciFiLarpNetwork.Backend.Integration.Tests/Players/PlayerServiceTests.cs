using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Players;

[Collection("Backend database integration")]
public sealed class PlayerServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private IPlayerService Players => factory.Services.GetRequiredService<IPlayerService>();

    private ICharacterService Characters => factory.Services.GetRequiredService<ICharacterService>();

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreatedPlayerKeepsEveryField()
    {
        var result = await Players.CreatePlayerAsync(
            new PlayerDefinition("  Ann Example ", " ann@example.org ", " 555 0100 ", " Brings props ", PlayerType.GameMaster),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new PlayerSummary(result.Value!.PlayerId, 1, "Ann Example", "ann@example.org", "555 0100", "Brings props",
                PlayerType.GameMaster, PlayerStatus.Active, false, 1),
            result.Value);
    }

    [Fact]
    public async Task PlayersGetRunningNumbersInCreationOrder()
    {
        var created = new List<int>();
        foreach (var name in new[] { "Ann", "Bob", "Cai" })
        {
            created.Add((await CreatePlayerAsync(name)).Number);
        }

        Assert.Equal([1, 2, 3], created);
    }

    [Fact]
    public async Task InvalidNewPlayerIsRejectedAndNotStored()
    {
        var result = await Players.CreatePlayerAsync(Definition(" "), TestContext.Current.CancellationToken);

        var stored = await Players.GetPlayersAsync(new RosterQuery(null, true, 1), TestContext.Current.CancellationToken);
        Assert.Equal(new { Outcome = RosterOutcome.InvalidName, StoredCount = 0L }, new { result.Outcome, StoredCount = stored.TotalCount });
    }

    [Fact]
    public async Task BlankOptionalDetailsAreNotSet()
    {
        var result = await Players.CreatePlayerAsync(
            new PlayerDefinition("Ann", " ", "", "  ", PlayerType.Player), TestContext.Current.CancellationToken);

        Assert.Equal(new { Email = (string?)null, Phone = (string?)null, Notes = (string?)null },
            new { result.Value!.Email, result.Value.Phone, result.Value.Notes });
    }

    [Fact]
    public async Task EditChangesTheDetails()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Players.UpdatePlayerAsync(player.PlayerId, player.Version,
            new PlayerDefinition("Ann B", "ann@example.org", null, "Notes", PlayerType.Senior), TestContext.Current.CancellationToken);

        Assert.Equal(
            player with { DisplayName = "Ann B", Email = "ann@example.org", Notes = "Notes", Type = PlayerType.Senior, Version = 2 },
            result.Value);
    }

    [Fact]
    public async Task InvalidEditIsRejectedAndChangesNothing()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Players.UpdatePlayerAsync(player.PlayerId, player.Version,
            Definition("Ann B") with { Email = "not an email" }, TestContext.Current.CancellationToken);

        var stored = await Players.GetPlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);
        Assert.Equal(new { Outcome = RosterOutcome.InvalidEmail, Stored = (PlayerSummary?)player }, new { result.Outcome, Stored = stored });
    }

    [Fact]
    public async Task UnchangedEditKeepsTheVersion()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Players.UpdatePlayerAsync(player.PlayerId, player.Version, Definition("Ann"), TestContext.Current.CancellationToken);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task EditStartedBeforeACharacterWasCreatedIsStale()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");

        var result = await Players.UpdatePlayerAsync(player.PlayerId, player.Version, Definition("Ann B"), TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task StaleEditIsRejected()
    {
        var player = await CreatePlayerAsync("Ann");
        await Players.UpdatePlayerAsync(player.PlayerId, player.Version, Definition("Ann B"), TestContext.Current.CancellationToken);

        var result = await Players.UpdatePlayerAsync(player.PlayerId, player.Version, Definition("Ann C"), TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task EditOfAnUnknownPlayerIsNotFound()
    {
        var result = await Players.UpdatePlayerAsync(Guid.NewGuid(), 1, Definition("Ann"), TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task DeactivationRetiresOnlyTheActiveCharacters()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");
        var dead = await CreateCharacterAsync(player, "Vel");
        await Characters.UpdateCharacterAsync(dead.CharacterId, dead.Version,
            CharacterDefinitions.From(dead) with { Status = CharacterStatus.Dead, DeathDate = new DateOnly(2026, 5, 1) },
            TestContext.Current.CancellationToken);

        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(
            [CharacterStatus.Retired, CharacterStatus.Dead],
            (await Characters.GetCharactersOfPlayerAsync(player.PlayerId, false, TestContext.Current.CancellationToken))
            .Select(character => character.Status));
    }

    [Fact]
    public async Task ActivationKeepsRetiredCharactersRetired()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        await Players.ActivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(
            [CharacterStatus.Retired],
            (await Characters.GetCharactersOfPlayerAsync(player.PlayerId, false, TestContext.Current.CancellationToken))
            .Select(character => character.Status));
    }

    [Fact]
    public async Task ActivePlayerCannotBeArchived()
    {
        var player = await CreatePlayerAsync("Ann");

        var result = await Players.ArchivePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.PlayerNotInactive, result.Outcome);
    }

    [Fact]
    public async Task InactivePlayerCanBeArchived()
    {
        var player = await CreatePlayerAsync("Ann");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        var result = await Players.ArchivePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(new { Status = PlayerStatus.Inactive, IsArchived = true }, new { result.Value!.Status, result.Value.IsArchived });
    }

    [Fact]
    public async Task ArchivedPlayerCannotBeActivated()
    {
        var player = await CreatePlayerAsync("Ann");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);
        await Players.ArchivePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        var result = await Players.ActivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(RosterOutcome.Archived, result.Outcome);
    }

    [Fact]
    public async Task UnarchivedPlayerIsListedAgain()
    {
        var player = await CreatePlayerAsync("Ann");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);
        await Players.ArchivePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        await Players.UnarchivePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(["Ann"], await NamesAsync(new RosterQuery(null, false, 1)));
    }

    [Fact]
    public async Task ArchivedPlayersAreHiddenUnlessRequested()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreatePlayerAsync("Bob");
        await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);
        await Players.ArchivePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Default = "Bob", All = "Ann, Bob" },
            new
            {
                Default = string.Join(", ", await NamesAsync(new RosterQuery(null, false, 1))),
                All = string.Join(", ", await NamesAsync(new RosterQuery(null, true, 1)))
            });
    }

    [Fact]
    public async Task SearchMatchesPartOfTheNameIgnoringCase()
    {
        await CreatePlayerAsync("Ann Example");
        await CreatePlayerAsync("Bob");

        Assert.Equal(["Ann Example"], await NamesAsync(new RosterQuery("EXAM", false, 1)));
    }

    [Fact]
    public async Task SearchMatchesTheRunningNumber()
    {
        await CreatePlayerAsync("Ann");
        await CreatePlayerAsync("Bob");

        Assert.Equal(["Bob"], await NamesAsync(new RosterQuery("2", false, 1)));
    }

    [Fact]
    public async Task SearchTreatsWildcardCharactersAsText()
    {
        await CreatePlayerAsync("Ann");
        await CreatePlayerAsync("Bob_100%");

        Assert.Equal(["Bob_100%"], await NamesAsync(new RosterQuery("_100%", false, 1)));
    }

    [Fact]
    public async Task PagesHoldFiftyPlayersInRunningNumberOrder()
    {
        for (var index = 1; index <= RosterQuery.PageSize + 1; index++)
        {
            await CreatePlayerAsync($"Player {index}");
        }

        var page = await Players.GetPlayersAsync(new RosterQuery(null, false, 2), TestContext.Current.CancellationToken);

        Assert.Equal(
            new { Names = "Player 51", PageNumber = 2, PageCount = 2, TotalCount = 51L },
            new { Names = string.Join(", ", page.Items.Select(player => player.DisplayName)), page.PageNumber, page.PageCount, page.TotalCount });
    }

    [Fact]
    public async Task EmptyListHasOneEmptyPage()
    {
        var page = await Players.GetPlayersAsync(new RosterQuery(null, false, 1), TestContext.Current.CancellationToken);

        Assert.Equal(new { Count = 0, PageCount = 1 }, new { page.Items.Count, page.PageCount });
    }

    [Fact]
    public async Task OverlappingCreationsBothSucceedWithTheirOwnRunningNumbers()
    {
        await CreatePlayerAsync("Ann");
        using var overlap = new Barrier(2);
        factory.Clock.FirstReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Players.CreatePlayerAsync(Definition("Bob"), TestContext.Current.CancellationToken)),
            Task.Run(() => Players.CreatePlayerAsync(Definition("Cai"), TestContext.Current.CancellationToken)));

        Assert.Equal([2, 3], results.Select(result => result.Value?.Number).Order());
    }

    [Fact]
    public async Task FirstEverOverlappingCreationsBothSucceedWithTheirOwnRunningNumbers()
    {
        using var overlap = new Barrier(2);
        factory.Clock.FirstReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Players.CreatePlayerAsync(Definition("Ann"), TestContext.Current.CancellationToken)),
            Task.Run(() => Players.CreatePlayerAsync(Definition("Bob"), TestContext.Current.CancellationToken)));

        Assert.Equal([1, 2], results.Select(result => result.Value?.Number).Order());
    }

    [Fact]
    public async Task DeactivationOverlappingACharacterCreationLetsOnlyOneSucceed()
    {
        var player = await CreatePlayerAsync("Ann");
        await CreateCharacterAsync(player, "Kira");
        using var overlap = new Barrier(2);
        factory.Clock.FirstReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(async () => (await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken)).Outcome),
            Task.Run(async () => (await Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named("Vel"),
                TestContext.Current.CancellationToken)).Outcome));

        Assert.Contains(string.Join(',', results.Order()), (string[])["Succeeded,ConcurrentChange", "Succeeded,PlayerInactive"]);
    }

    [Fact]
    public async Task DeactivationOverlappingACharacterActivationLetsOnlyOneSucceed()
    {
        var player = await CreatePlayerAsync("Ann");
        var character = await CreateCharacterAsync(player, "Kira");
        var retired = (await Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Retired },
            TestContext.Current.CancellationToken)).Value!;
        using var overlap = new Barrier(2);
        factory.Clock.ReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(async () => (await Players.DeactivatePlayerAsync(player.PlayerId, TestContext.Current.CancellationToken)).Outcome),
            Task.Run(async () => (await Characters.UpdateCharacterAsync(retired.CharacterId, retired.Version,
                CharacterDefinitions.From(retired) with { Status = CharacterStatus.Active },
                TestContext.Current.CancellationToken)).Outcome));
        factory.Clock.ReadBarrier = null;

        Assert.Equal([RosterOutcome.Succeeded, RosterOutcome.ConcurrentChange], results.Order());
    }

    private static PlayerDefinition Definition(string name) => new(name, null, null, null, PlayerType.Player);

    private async Task<PlayerSummary> CreatePlayerAsync(string name) =>
        (await Players.CreatePlayerAsync(Definition(name), TestContext.Current.CancellationToken)).Value!;

    private async Task<CharacterSummary> CreateCharacterAsync(PlayerSummary player, string name) =>
        (await Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named(name),
            TestContext.Current.CancellationToken)).Value!;

    private async Task<string[]> NamesAsync(RosterQuery query) =>
        (await Players.GetPlayersAsync(query, TestContext.Current.CancellationToken)).Items
        .Select(player => player.DisplayName)
        .ToArray();
}
