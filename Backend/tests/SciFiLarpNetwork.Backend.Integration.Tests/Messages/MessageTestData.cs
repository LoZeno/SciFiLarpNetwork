using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Messages;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Backend.Tests.Integration.Players;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Messages;

/// <summary>Creates characters and messages through the public services.</summary>
internal sealed class MessageTestData(AdministratorAuthenticationFactory factory)
{
    public IMessageService Messages => factory.Services.GetRequiredService<IMessageService>();

    public ICharacterService Characters => factory.Services.GetRequiredService<ICharacterService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async Task<CharacterSummary> CreateCharacterAsync(string name = "Kira", CharacterStatus status = CharacterStatus.Active)
    {
        var players = factory.Services.GetRequiredService<IPlayerService>();
        var player = (await players.CreatePlayerAsync(new PlayerDefinition("Ann", null, null, null, PlayerType.Player), Token)).Value!;
        var created = (await Characters.CreateCharacterAsync(player.PlayerId, CharacterDefinitions.Named(name), Token)).Value!;
        if (status == CharacterStatus.Active)
        {
            return created;
        }

        var definition = CharacterDefinitions.From(created) with
        {
            Status = status,
            DeathDate = status == CharacterStatus.Dead ? new DateOnly(2026, 5, 1) : null
        };
        return (await Characters.UpdateCharacterAsync(created.CharacterId, created.Version, definition, Token)).Value!;
    }

    public async Task<CharacterSummary> CreateArchivedCharacterAsync(string name = "Old Tom")
    {
        var retired = await CreateCharacterAsync(name, CharacterStatus.Retired);
        return (await Characters.ArchiveCharacterAsync(retired.CharacterId, Token)).Value!;
    }

    public static MessageDraft Draft(CharacterSummary sender, params CharacterSummary[] recipients) =>
        new(Guid.NewGuid(), sender.CharacterId, recipients.Select(recipient => recipient.CharacterId).ToArray(),
            "Supply run", "Meet at the dock.\nBring crates.");

    public async Task<MessageDetails> SendAsync(MessageDraft draft) =>
        (await Messages.SendMessageAsync(draft, Token)).Value!;
}
