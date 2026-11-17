namespace SciFiLarpNetwork.Backend.Application.Players;

/// <param name="Search">
/// Matches names containing the text (ignoring case), or the running number when the text is a whole number;
/// blank matches everything.
/// </param>
/// <param name="PageNumber">The one-based page; pages hold <see cref="PageSize"/> entries ordered by running number.</param>
public sealed record RosterQuery(string? Search, bool IncludeArchived, int PageNumber)
{
    public const int PageSize = 50;
}
