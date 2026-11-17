using System.Globalization;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Skills;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

/// <summary>The search text of a player or character query, in the forms the stored documents are matched against.</summary>
internal sealed record RosterSearch(string? NormalizedText, int? Number)
{
    public static RosterSearch From(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new RosterSearch(null, null);
        }

        var number = int.TryParse(search.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (int?)null;
        return new RosterSearch(CatalogNames.Normalize(search), number);
    }

    public static RosterPage<TSummary> ToPage<TDocument, TSummary>(
        IPagedList<TDocument> page,
        IReadOnlyList<TSummary> items) =>
        new(items, (int)page.PageNumber, Math.Max(1, (int)page.PageCount), page.TotalItemCount);
}
