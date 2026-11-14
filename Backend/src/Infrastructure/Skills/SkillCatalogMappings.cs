using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

internal static class SkillCatalogMappings
{
    extension(TalentDocument talent)
    {
        public TalentSummary ToSummary() =>
            new(talent.Id, talent.Name, talent.Description, talent.IsArchived, talent.Version);
    }

    extension(SkillDocument skill)
    {
        public SkillSummary ToSummary() =>
            new(
                skill.Id,
                skill.Name,
                skill.Description,
                skill.Cost,
                skill.IsMultiPurchase,
                skill.IsAdvanced,
                skill.TalentIds,
                skill.GameFlags,
                skill.CardFlagIds,
                skill.PrerequisiteGroups,
                skill.IsArchived,
                skill.Version);
    }

    extension(CardFlagDocument cardFlag)
    {
        public CardFlagSummary ToSummary() => new(cardFlag.Id, cardFlag.Name);
    }
}
