using SciFiLarpNetwork.Backend.Application.Items;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemCards;

/// <param name="QrSvg">The QR code drawn as an SVG image.</param>
internal sealed record TemplateItemCard(
    string ItemType,
    string Name,
    string QrCode,
    string QrSvg,
    string? Description,
    string? Effect,
    int? Charges,
    DateOnly? ExpiresOn,
    bool IsArchived)
{
    public static TemplateItemCard From(ItemCard card, string qrSvg) =>
        new(card.ItemType, card.Name, card.QrCode, qrSvg, card.Description, card.Effect, card.Charges, card.ExpiresOn,
            card.IsArchived);
}
