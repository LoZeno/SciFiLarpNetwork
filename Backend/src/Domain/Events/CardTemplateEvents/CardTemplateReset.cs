namespace SciFiLarpNetwork.Backend.Domain.Events.CardTemplateEvents;

/// <summary>Records a return to the default template; the uploaded template's content is kept for the audit trail.</summary>
public sealed record CardTemplateReset(string CardKind, DateTimeOffset OccurredAtUtc);
