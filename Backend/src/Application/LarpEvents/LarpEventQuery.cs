namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <param name="PageNumber">
/// The one-based page of <see cref="PageSize"/> events: newest first day first, events without days before the
/// others, then newest running number first.
/// </param>
public sealed record LarpEventQuery(bool IncludeArchived, int PageNumber)
{
    public const int PageSize = 50;
}
