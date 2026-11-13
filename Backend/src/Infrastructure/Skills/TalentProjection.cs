using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

public partial class TalentProjection : SingleStreamProjection<TalentDocument, Guid>
{
    public TalentProjection()
    {
        IncludeType<TalentCreated>();
        IncludeType<TalentDetailsChanged>();
        IncludeType<TalentArchived>();
        IncludeType<TalentUnarchived>();
    }

    public override TalentDocument? Evolve(TalentDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            TalentCreated created => new TalentDocument(
                created.TalentId,
                created.Name,
                created.NormalizedName,
                created.Description,
                IsArchived: false),
            TalentDetailsChanged changed when snapshot is not null => snapshot with
            {
                Name = changed.Name,
                NormalizedName = changed.NormalizedName,
                Description = changed.Description
            },
            TalentArchived when snapshot is not null => snapshot with { IsArchived = true },
            TalentUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
