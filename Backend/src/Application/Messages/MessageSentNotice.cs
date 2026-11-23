namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <summary>
/// Tells live clients that a message was sent. It carries no title or text: a client that wants the message reloads it
/// through the API, which authorizes the request and records any opening.
/// </summary>
/// <param name="RecipientIds">The recipients, so a client listing one character's incoming messages can tell whether it changed.</param>
public sealed record MessageSentNotice(Guid MessageId, IReadOnlyList<Guid> RecipientIds);
