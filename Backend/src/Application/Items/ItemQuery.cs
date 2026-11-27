namespace SciFiLarpNetwork.Backend.Application.Items;

/// <param name="Search">Matches items whose name, description or effect contains the text, ignoring case; blank matches
/// everything.</param>
/// <param name="ItemTypeId">Only items of this type, or every type when <see langword="null"/>.</param>
/// <param name="PageNumber">The one-based page; pages hold <see cref="PageSize"/> items ordered by name.</param>
public sealed record ItemQuery(string? Search, Guid? ItemTypeId, bool IncludeArchived, int PageNumber)
{
    public const int PageSize = 50;
}
