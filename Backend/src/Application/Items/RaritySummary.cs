namespace SciFiLarpNetwork.Backend.Application.Items;

/// <param name="Position">The rarity's zero-based place in the administrator-set order.</param>
public sealed record RaritySummary(Guid RarityId, string Name, int Position, bool IsArchived, long Version);
