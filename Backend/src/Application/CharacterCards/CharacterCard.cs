namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>Everything printed on one character card.</summary>
/// <param name="QrIdentifier">The character's public QR identifier, printed as a QR image.</param>
/// <param name="CardFlags">Every card flag, in name order, with the character's value; 0 when no owned skill has it.</param>
/// <param name="Stats">Stats that are not archived, in name order; Text stats with an empty value are left out.</param>
/// <param name="Talents">The character's talents, in the order they were picked, with the skills bought through each.</param>
public sealed record CharacterCard(
    int PlayerNumber,
    string PlayerName,
    int CharacterNumber,
    string CharacterName,
    string? Title,
    string? Gender,
    string? Species,
    string? Type,
    string QrIdentifier,
    int ExperiencePoints,
    long FreePoints,
    bool IsArchived,
    IReadOnlyList<CardFlagValue> CardFlags,
    IReadOnlyList<CardStatValue> Stats,
    IReadOnlyList<CardTalent> Talents);
