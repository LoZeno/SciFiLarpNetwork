namespace SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;

/// <summary>Records every new detail of an edit in one event; components and the image have events of their own.</summary>
public sealed record ItemDetailsChanged(
    Guid ItemId,
    string Name,
    string NormalizedName,
    Guid ItemTypeId,
    Guid RarityId,
    string? Description,
    string? Effect,
    int Cost,
    int? Charges,
    DateOnly? ExpiresOn,
    DateTimeOffset OccurredAtUtc);
