using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

public partial class SkillProjection : SingleStreamProjection<SkillDocument, Guid>
{
    public SkillProjection()
    {
        IncludeType<SkillCreated>();
        IncludeType<SkillDetailsChanged>();
        IncludeType<SkillTalentsChanged>();
        IncludeType<SkillGameFlagsChanged>();
        IncludeType<SkillCardFlagsChanged>();
        IncludeType<SkillPrerequisitesChanged>();
        IncludeType<SkillArchived>();
        IncludeType<SkillUnarchived>();
    }

    public override SkillDocument? Evolve(SkillDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            SkillCreated created => new SkillDocument(
                created.SkillId,
                created.Name,
                created.NormalizedName,
                created.Description,
                created.Cost,
                created.IsMultiPurchase,
                created.IsAdvanced,
                created.TalentIds,
                created.GameFlags,
                created.CardFlagIds,
                created.PrerequisiteGroups,
                IsArchived: false),
            SkillDetailsChanged changed when snapshot is not null => snapshot with
            {
                Name = changed.Name,
                NormalizedName = changed.NormalizedName,
                Description = changed.Description,
                Cost = changed.Cost,
                IsMultiPurchase = changed.IsMultiPurchase,
                IsAdvanced = changed.IsAdvanced
            },
            SkillTalentsChanged changed when snapshot is not null => snapshot with { TalentIds = changed.TalentIds },
            SkillGameFlagsChanged changed when snapshot is not null => snapshot with { GameFlags = changed.GameFlags },
            SkillCardFlagsChanged changed when snapshot is not null => snapshot with { CardFlagIds = changed.CardFlagIds },
            SkillPrerequisitesChanged changed when snapshot is not null => snapshot with { PrerequisiteGroups = changed.PrerequisiteGroups },
            SkillArchived when snapshot is not null => snapshot with { IsArchived = true },
            SkillUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
