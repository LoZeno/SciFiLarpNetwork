using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Backend.Tests.Integration.Players;

namespace SciFiLarpNetwork.Backend.Tests.Integration.LarpEvents;

/// <summary>Creates events, days, and characters through the public services.</summary>
internal sealed class LarpEventTestData(AdministratorAuthenticationFactory factory)
{
    public static readonly DateOnly FirstDay = new(2026, 5, 1);

    public ILarpEventService Events => factory.Services.GetRequiredService<ILarpEventService>();

    public ILarpEventDayService Days => factory.Services.GetRequiredService<ILarpEventDayService>();

    public ILarpEventParticipantService Participants => factory.Services.GetRequiredService<ILarpEventParticipantService>();

    public ICharacterService Characters => factory.Services.GetRequiredService<ICharacterService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async Task<LarpEventSummary> CreateEventAsync(string name = "Fort weekend", int dayCount = 0)
    {
        var created = (await Events.CreateEventAsync(new LarpEventDefinition(name, null), Token)).Value!;
        for (var day = 0; day < dayCount; day++)
        {
            created = (await Days.AddDayAsync(created.EventId, FirstDay.AddDays(day), Token)).Value!;
        }

        return created;
    }

    public async Task<CharacterSummary> CreateCharacterAsync(string name = "Kira", int experiencePoints = 0)
    {
        var players = factory.Services.GetRequiredService<IPlayerService>();
        var player = (await players.CreatePlayerAsync(new PlayerDefinition("Ann", null, null, null, PlayerType.Player), Token)).Value!;
        return (await Characters.CreateCharacterAsync(player.PlayerId,
            CharacterDefinitions.Named(name) with { ExperiencePoints = experiencePoints }, Token)).Value!;
    }

    /// <summary>Makes the character own one skill of <paramref name="cost"/> points.</summary>
    public async Task BuySkillAsync(CharacterSummary character, int cost)
    {
        var talent = (await factory.Services.GetRequiredService<ITalentService>()
            .CreateTalentAsync(new TalentDefinition("Pilot", null), Token)).Value!;
        var skill = (await factory.Services.GetRequiredService<ISkillService>().CreateSkillAsync(
            new SkillDefinition("Flying", null, cost, false, false, [talent.TalentId], [], [], [], []), Token)).Value!;
        var current = await Characters.GetCharacterAsync(character.CharacterId, Token);
        await factory.Services.GetRequiredService<ICharacterSkillService>().SaveCharacterSkillsAsync(
            character.CharacterId,
            current!.Version,
            new CharacterSkillsDefinition([talent.TalentId], [new SkillPurchase(skill.SkillId, talent.TalentId, 1, null)]),
            Token);
    }

    public async Task<LarpEventSummary> CloseAsync(LarpEventSummary larpEvent) =>
        (await Events.CloseEventAsync(larpEvent.EventId, Token)).Value!;

    public static Guid[] AllDays(LarpEventSummary larpEvent) => larpEvent.Days.Select(day => day.DayId).ToArray();
}
