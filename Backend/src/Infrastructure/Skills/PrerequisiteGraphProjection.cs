using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

public partial class PrerequisiteGraphProjection : SingleStreamProjection<PrerequisiteGraphDocument, Guid>
{
    public PrerequisiteGraphProjection()
    {
        IncludeType<PrerequisiteGraphChanged>();
    }

    public override PrerequisiteGraphDocument? Evolve(PrerequisiteGraphDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            PrerequisiteGraphChanged changed => new PrerequisiteGraphDocument(id, changed.SkillId),
            _ => snapshot
        };
}
