namespace SciFiLarpNetwork.Management.Api.Players;

internal sealed record PlayerProperties(
    Guid PlayerId,
    int Number,
    string DisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    string Type,
    string Status,
    bool IsArchived,
    long Version);
