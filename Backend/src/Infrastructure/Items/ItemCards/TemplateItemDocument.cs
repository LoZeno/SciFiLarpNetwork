namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemCards;

/// <summary>The <c>cards.json</c> file an item-card template reads; its shape is a contract with template authors.</summary>
internal sealed record TemplateItemDocument(
    string Language,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<TemplateItemCard> Cards);
