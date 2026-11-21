using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

internal static class LarpEventMappings
{
    extension(LarpEventDocument larpEvent)
    {
        public LarpEventSummary ToSummary() =>
            new(
                larpEvent.Id,
                larpEvent.Number,
                larpEvent.Name,
                larpEvent.Description,
                larpEvent.Status,
                larpEvent.IsArchived,
                larpEvent.BaseExperience,
                larpEvent.Days.Length == 0 ? null : larpEvent.Days[0].Date,
                larpEvent.Days.Length == 0 ? null : larpEvent.Days[^1].Date,
                larpEvent.Days
                    .Select(day => new LarpEventDaySummary(
                        day.DayId,
                        day.Date,
                        day.Experience,
                        larpEvent.Participants.Count(participant => participant.DayIds.Contains(day.DayId))))
                    .ToArray(),
                larpEvent.Participants.Length,
                larpEvent.Version);

        public LarpEventParticipant? Participant(Guid characterId) =>
            larpEvent.Participants.FirstOrDefault(participant => participant.CharacterId == characterId);

        public bool HasDay(Guid dayId) => larpEvent.Days.Any(day => day.DayId == dayId);

        /// <summary>Whether a close already gave this day's experience to anyone, so it must not change or be removed.</summary>
        public bool WasDayExperienceGiven(Guid dayId) => larpEvent.GivenAwards.Any(award => award.DayIds.Contains(dayId));

        public LarpEventGivenAward GivenAward(Guid characterId) =>
            larpEvent.GivenAwards.FirstOrDefault(award => award.CharacterId == characterId)
            ?? new LarpEventGivenAward(characterId, false, []);

        /// <param name="playerNames">Player display names by player identifier.</param>
        /// <param name="administratorNames">Administrator user names by the recorded account identifier.</param>
        public ParticipantSummary ToParticipantSummary(
            LarpEventParticipant participant,
            CharacterDocument character,
            IReadOnlyDictionary<Guid, string> playerNames,
            IReadOnlyDictionary<string, string> administratorNames)
        {
            var given = larpEvent.GivenAward(participant.CharacterId);
            var days = larpEvent.Days.Where(day => participant.DayIds.Contains(day.DayId)).ToArray();
            var changes = larpEvent.ExperienceChanges
                .Where(change => change.CharacterId == participant.CharacterId)
                .Select(change => new ExperienceChangeSummary(
                    change.Amount,
                    change.ChangedBy is { } recorded ? administratorNames.GetValueOrDefault(recorded, recorded) : null,
                    change.ChangedAtUtc,
                    change.IsClosingAward))
                .ToArray();
            return new ParticipantSummary(
                character.Id,
                character.Number,
                character.Name,
                character.PlayerId,
                playerNames.GetValueOrDefault(character.PlayerId, string.Empty),
                character.Status,
                character.IsArchived,
                character.ExperiencePoints,
                days.Select(day => day.DayId).ToArray(),
                days.Select(day => day.Date).ToArray(),
                changes.Sum(change => (long)change.Amount),
                changes,
                given.BaseExperience,
                given.DayIds);
        }
    }
}
