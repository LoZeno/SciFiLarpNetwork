using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.CharacterSkills;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

/// <param name="StatValues">Only the stats this character has a value for, keyed by stat identifier.</param>
public sealed record CharacterDocument(
    Guid Id,
    int Number,
    Guid PlayerId,
    string Name,
    string NormalizedName,
    string? Title,
    string? Species,
    string? Gender,
    string? Type,
    int ExperiencePoints,
    CharacterStatus Status,
    DateOnly? DeathDate,
    Guid? NetworkId,
    string QrIdentifier,
    CharacterStatValue[] StatValues,
    bool IsArchived)
{
    /// <summary>The character stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }

    /// <summary>The character's talents, in the order they were picked.</summary>
    public Guid[] TalentIds { get; init; } = [];

    public SkillPurchase[] SkillPurchases { get; init; } = [];
}
