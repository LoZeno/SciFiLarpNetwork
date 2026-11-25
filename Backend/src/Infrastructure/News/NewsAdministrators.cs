using Marten;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.News;

/// <summary>
/// The user names of recorded administrator actors. A recorded value that is not a known account is shown as recorded.
/// </summary>
internal sealed class NewsAdministrators(IReadOnlyDictionary<Guid, string> names)
{
    public static async Task<NewsAdministrators> LoadAsync(
        IQuerySession session,
        IEnumerable<string?> recorded,
        CancellationToken cancellationToken)
    {
        Guid[] accountIds = [.. recorded.Select(AccountId).OfType<Guid>().Distinct()];
        if (accountIds.Length == 0)
        {
            return new NewsAdministrators(new Dictionary<Guid, string>());
        }

        var accounts = await session.LoadManyAsync<AdministratorAccountDocument>(cancellationToken, accountIds);
        return new NewsAdministrators(accounts.ToDictionary(account => account.Id, account => account.UserName));
    }

    public string? Describe(string? recorded) =>
        AccountId(recorded) is { } accountId && names.TryGetValue(accountId, out var name) ? name : recorded;

    private static Guid? AccountId(string? recorded) => Guid.TryParse(recorded, out var id) ? id : null;
}
