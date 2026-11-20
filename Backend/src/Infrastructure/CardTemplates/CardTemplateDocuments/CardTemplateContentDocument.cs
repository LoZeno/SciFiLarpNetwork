namespace SciFiLarpNetwork.Backend.Infrastructure.CardTemplates.CardTemplateDocuments;

/// <summary>
/// An uploaded template source, stored as its own document so template text never enters the event stream.
/// Documents are written once and never changed or deleted, so every upload stays auditable.
/// </summary>
public sealed record CardTemplateContentDocument(Guid Id, string CardKind, byte[] Content);
