namespace SciFiLarpNetwork.Backend.Domain.Events.RarityEvents;

/// <summary>Appended to the single rarity-order stream with the whole new order, first rarity first.</summary>
public sealed record RarityOrderChanged(Guid[] RarityIds, DateTimeOffset OccurredAtUtc);
