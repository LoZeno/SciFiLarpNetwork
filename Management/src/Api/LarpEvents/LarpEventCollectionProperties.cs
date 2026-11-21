using SciFiLarpNetwork.Management.Api.Players;

namespace SciFiLarpNetwork.Management.Api.LarpEvents;

internal sealed record LarpEventCollectionProperties(
    string Title,
    string Description,
    bool IncludesArchived,
    int PageNumber,
    int PageCount,
    long TotalCount,
    IReadOnlyList<RosterOptionProperties> Statuses);
