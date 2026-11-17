namespace SciFiLarpNetwork.Backend.Api.Players;

/// <summary>A page of players or characters.</summary>
/// <param name="Search">The search text the page matches, or <see langword="null"/> for every entry.</param>
/// <param name="PlayerTypes">Every player type with its localized title, for player dialogs.</param>
/// <param name="CharacterStatuses">Every character status with its localized title, for character dialogs.</param>
/// <param name="StatTypes">Every stat type with its localized title, for character dialogs.</param>
public sealed record RosterCollectionProperties(
    string Title,
    string Description,
    string? Search,
    bool IncludesArchived,
    int PageNumber,
    int PageCount,
    long TotalCount,
    IReadOnlyList<RosterOption> PlayerTypes,
    IReadOnlyList<RosterOption> CharacterStatuses,
    IReadOnlyList<RosterOption> StatTypes);
