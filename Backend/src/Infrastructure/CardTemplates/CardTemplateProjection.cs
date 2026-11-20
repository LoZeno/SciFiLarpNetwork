using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.CardTemplateEvents;
using SciFiLarpNetwork.Backend.Infrastructure.CardTemplates.CardTemplateDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.CardTemplates;

public partial class CardTemplateProjection : SingleStreamProjection<CardTemplateDocument, Guid>
{
    public CardTemplateProjection()
    {
        IncludeType<CardTemplateUploaded>();
        IncludeType<CardTemplateReset>();
    }

    public override CardTemplateDocument? Evolve(CardTemplateDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            CardTemplateUploaded uploaded => new CardTemplateDocument(id, uploaded.ContentId, e.UserName, uploaded.OccurredAtUtc),
            CardTemplateReset => new CardTemplateDocument(id, null, null, null),
            _ => snapshot
        };
}
