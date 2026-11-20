namespace SciFiLarpNetwork.Backend.Domain.Events.CardTemplateEvents;

/// <summary>
/// Records an uploaded template by reference; the template source is stored outside the event stream. The uploader
/// is the event's user name metadata.
/// </summary>
/// <param name="CardKind">The kind of card the template prints, for example <c>character</c>.</param>
public sealed record CardTemplateUploaded(string CardKind, Guid ContentId, int ByteCount, DateTimeOffset OccurredAtUtc);
