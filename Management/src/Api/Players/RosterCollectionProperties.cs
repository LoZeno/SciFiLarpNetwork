namespace SciFiLarpNetwork.Management.Api.Players;

internal sealed record RosterCollectionProperties(
    string Title,
    string Description,
    string? Search,
    bool IncludesArchived,
    int PageNumber,
    int PageCount,
    long TotalCount,
    IReadOnlyList<RosterOptionProperties> PlayerTypes,
    IReadOnlyList<RosterOptionProperties> CharacterStatuses,
    IReadOnlyList<RosterOptionProperties> StatTypes);
