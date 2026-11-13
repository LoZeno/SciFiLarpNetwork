using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.CardFlagEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Skills;

public partial class CardFlagProjection : SingleStreamProjection<CardFlagDocument, Guid>
{
    public CardFlagProjection()
    {
        IncludeType<CardFlagDefined>();
    }

    public override CardFlagDocument? Evolve(CardFlagDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            CardFlagDefined defined => new CardFlagDocument(defined.CardFlagId, defined.Name, defined.NormalizedName),
            _ => snapshot
        };
}
