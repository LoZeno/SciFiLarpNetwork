using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.TemplateData;

/// <param name="QrSvg">The QR identifier drawn as an SVG image.</param>
internal sealed record TemplateCard(
    int PlayerNumber,
    string PlayerName,
    int CharacterNumber,
    string CharacterName,
    string? Title,
    string? Gender,
    string? Species,
    string? Type,
    string QrIdentifier,
    string QrSvg,
    int ExperiencePoints,
    long FreePoints,
    bool IsArchived,
    IReadOnlyList<CardFlagValue> CardFlags,
    IReadOnlyList<CardStatValue> Stats,
    IReadOnlyList<CardTalent> Talents)
{
    public static TemplateCard From(CharacterCard card, string qrSvg) =>
        new(
            card.PlayerNumber,
            card.PlayerName,
            card.CharacterNumber,
            card.CharacterName,
            card.Title,
            card.Gender,
            card.Species,
            card.Type,
            card.QrIdentifier,
            qrSvg,
            card.ExperiencePoints,
            card.FreePoints,
            card.IsArchived,
            card.CardFlags,
            card.Stats,
            card.Talents);
}
