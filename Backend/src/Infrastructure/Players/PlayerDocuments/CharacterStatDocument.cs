using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

/// <param name="DefaultNumber">The value of a Number stat for characters without their own value.</param>
/// <param name="DefaultText">The value of a Text stat for characters without their own value.</param>
public sealed record CharacterStatDocument(
    Guid Id,
    string Name,
    string NormalizedName,
    CharacterStatType Type,
    int? DefaultNumber,
    string? DefaultText,
    bool IsArchived)
{
    /// <summary>The stat stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
