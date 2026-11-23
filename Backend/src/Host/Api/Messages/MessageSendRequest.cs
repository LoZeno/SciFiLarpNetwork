using System.Text.Json.Serialization;
using SciFiLarpNetwork.Backend.Application.Messages;

namespace SciFiLarpNetwork.Backend.Api.Messages;

/// <param name="MessageId">Chosen by the client once per message, so a repeated submission does not send it twice.</param>
/// <param name="ReplyToMessageId">The message this one replies to, if any.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MessageSendRequest(
    [property: JsonRequired] Guid MessageId,
    [property: JsonRequired] Guid SenderId,
    [property: JsonRequired] IReadOnlyList<Guid> RecipientIds,
    string? Title,
    string? Body,
    Guid? ReplyToMessageId)
{
    public MessageDraft ToDraft() => new(MessageId, SenderId, RecipientIds, Title, Body, ReplyToMessageId);
}
