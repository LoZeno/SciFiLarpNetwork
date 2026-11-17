using SciFiLarpNetwork.Backend.Domain.Characters;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

public sealed record CharacterStatDocument(
    Guid Id,
    string Name,
    string NormalizedName,
    CharacterStatType Type,
    bool IsArchived)
{
    /// <summary>The stat stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
