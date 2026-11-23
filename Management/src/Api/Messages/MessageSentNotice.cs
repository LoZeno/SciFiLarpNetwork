namespace SciFiLarpNetwork.Management.Api.Messages;

/// <summary>A message was sent. It carries no title or text; the screen reloads its list through the API.</summary>
public sealed record MessageSentNotice(Guid MessageId, IReadOnlyList<Guid> RecipientIds);
