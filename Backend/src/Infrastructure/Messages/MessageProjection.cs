using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.MessageEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Messages.MessageDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Messages;

/// <summary>A sent message never changes, so only <see cref="MessageSent"/> shapes the document.</summary>
public partial class MessageProjection : SingleStreamProjection<MessageDocument, Guid>
{
    public MessageProjection()
    {
        IncludeType<MessageSent>();
    }

    public override MessageDocument? Evolve(MessageDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            MessageSent sent => new MessageDocument(
                sent.MessageId,
                sent.SenderId,
                [.. sent.RecipientIds],
                sent.Title,
                sent.Body,
                sent.ReplyToMessageId,
                sent.OccurredAtUtc,
                e.UserName),
            _ => snapshot
        };
}
