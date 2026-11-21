using SciFiLarpNetwork.Backend.Api.Players;

namespace SciFiLarpNetwork.Backend.Api.LarpEvents;

/// <param name="Statuses">The event statuses with their localized titles.</param>
public sealed record LarpEventCollectionProperties(
    string Title,
    string Description,
    bool IncludesArchived,
    int PageNumber,
    int PageCount,
    long TotalCount,
    IReadOnlyList<RosterOption> Statuses);
