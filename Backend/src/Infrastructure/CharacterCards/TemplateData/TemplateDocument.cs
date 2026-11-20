namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.TemplateData;

/// <summary>The <c>cards.json</c> file a card template reads; its shape is a contract with template authors.</summary>
internal sealed record TemplateDocument(
    string Language,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<TemplateCard> Cards);
