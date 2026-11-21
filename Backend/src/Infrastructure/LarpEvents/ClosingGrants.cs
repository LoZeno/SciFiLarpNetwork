using SciFiLarpNetwork.Backend.Application.LarpEvents;
using SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents;

/// <summary>Checks the closing awards against the event and works out how much each participant gets.</summary>
internal static class ClosingGrants
{
    /// <returns>
    /// The grants of every award that picks anything, or <see cref="LarpEventOutcome.AwardNotAllowed"/> when an award
    /// names a character twice, a character that is not registered, a day the character is not registered for or that
    /// has no experience, base experience the event does not have, or experience an earlier close already gave.
    /// </returns>
    public static (LarpEventOutcome Outcome, IReadOnlyList<ClosingGrant> Grants) From(
        LarpEventDocument larpEvent,
        IReadOnlyList<ClosingAward> awards)
    {
        if (awards.Select(award => award.CharacterId).Distinct().Count() != awards.Count)
        {
            return (LarpEventOutcome.AwardNotAllowed, []);
        }

        var grants = new List<ClosingGrant>();
        foreach (var award in awards.Where(award => award.BaseExperience || award.DayIds.Count > 0))
        {
            var (outcome, grant) = Check(larpEvent, award);
            if (outcome != LarpEventOutcome.Succeeded)
            {
                return (outcome, []);
            }

            grants.Add(grant!);
        }

        return (LarpEventOutcome.Succeeded, grants);
    }

    private static (LarpEventOutcome Outcome, ClosingGrant? Grant) Check(LarpEventDocument larpEvent, ClosingAward award)
    {
        if (larpEvent.Participant(award.CharacterId) is not { } participant)
        {
            return (LarpEventOutcome.AwardNotAllowed, null);
        }

        var given = larpEvent.GivenAward(award.CharacterId);
        if (award.BaseExperience && (larpEvent.BaseExperience is null || given.BaseExperience))
        {
            return (LarpEventOutcome.AwardNotAllowed, null);
        }

        Guid[] dayIds = [.. award.DayIds.Distinct()];
        var days = larpEvent.Days.Where(day => dayIds.Contains(day.DayId)).ToArray();
        if (days.Length != dayIds.Length ||
            days.Any(day => day.Experience is null || !participant.DayIds.Contains(day.DayId) || given.DayIds.Contains(day.DayId)))
        {
            return (LarpEventOutcome.AwardNotAllowed, null);
        }

        var amount = (award.BaseExperience ? (long)larpEvent.BaseExperience!.Value : 0) + days.Sum(day => (long)day.Experience!.Value);
        return amount > int.MaxValue
            ? (LarpEventOutcome.ExperienceOutOfRange, null)
            : (LarpEventOutcome.Succeeded, new ClosingGrant(award.CharacterId, award.BaseExperience, dayIds, (int)amount));
    }
}
