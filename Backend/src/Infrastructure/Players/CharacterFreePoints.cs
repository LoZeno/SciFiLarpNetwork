using Marten;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

internal static class CharacterFreePoints
{
    /// <summary>
    /// Whether lowering the experience points would make free points negative. Free points that are already negative
    /// because a skill's cost was raised do not block other edits, or raising the experience points.
    /// </summary>
    public static async Task<bool> LeavesNegativeAsync(
        IQuerySession session,
        CharacterDocument character,
        int experiencePoints,
        CancellationToken cancellationToken)
    {
        if (experiencePoints >= character.ExperiencePoints || character.SkillPurchases.Length == 0)
        {
            return false;
        }

        var skillIds = character.SkillPurchases.Select(purchase => purchase.SkillId).ToArray();
        var costs = (await session.LoadManyAsync<SkillDocument>(cancellationToken, skillIds))
            .ToDictionary(skill => skill.Id, skill => skill.Cost);
        return CharacterSkillRules.FreePoints(experiencePoints, character.SkillPurchases, skillId => costs[skillId]) < 0;
    }
}
