using Marten;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal static class AdministratorQuerySessionExtensions
{
    extension(IQuerySession session)
    {
        public Task<AdministratorAccountDocument?> FindAccountByUserNameAsync(string userName, CancellationToken cancellationToken)
        {
            var normalizedUserName = AdministratorCredentialRules.NormalizeUserName(userName);
            return session.Query<AdministratorAccountDocument>()
                .Where(a => a.NormalizedUserName == normalizedUserName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<AdministratorSessionDocument?> FindSessionByTokenAsync(string sessionToken, CancellationToken cancellationToken)
        {
            var tokenHash = AdministratorSecrets.HashSecret(sessionToken);
            return session.Query<AdministratorSessionDocument>()
                .Where(s => s.TokenHash == tokenHash)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
