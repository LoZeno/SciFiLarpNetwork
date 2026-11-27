namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>The details of an item an administrator types; components and the image are changed separately.</summary>
public sealed record ItemDefinition(
    string Name,
    Guid ItemTypeId,
    Guid RarityId,
    string? Description,
    string? Effect,
    int Cost,
    int? Charges,
    DateOnly? ExpiresOn);
