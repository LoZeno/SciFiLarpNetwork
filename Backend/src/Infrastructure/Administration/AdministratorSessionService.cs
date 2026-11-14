using Marten;
using Microsoft.AspNetCore.Identity;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

internal sealed class AdministratorSessionService(
    AttributedSessionFactory sessions,
    IPasswordHasher<object> passwordHasher,
    TimeProvider timeProvider) : IAdministratorSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly object _passwordSubject = new();

    public async Task<AdministratorSession?> SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        AdministratorAccountDocument? account;
        await using (var query = sessions.QuerySession())
        {
            account = await query.FindAccountByUserNameAsync(userName, cancellationToken);
        }

        if (account is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(_passwordSubject, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Sign-in is anonymous at the HTTP level; the session events are attributed to the
        // account whose password was just verified, never to a client-supplied identity.
        await using var session = sessions.OpenSession(account.Id.ToString("D"));
        var now = timeProvider.GetUtcNow();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var accountStream = await session.Events.FetchForWriting<AdministratorAccountDocument>(account.Id, cancellationToken);
            accountStream.AppendOne(new AccountPasswordChanged(account.Id, passwordHasher.HashPassword(_passwordSubject, password), now));
        }

        var token = StartSession(session, account.Id, now, out var expiresAtUtc);
        await session.SaveChangesAsync(cancellationToken);

        return new AdministratorSession(account.Id, account.UserName, account.Role, expiresAtUtc, token);
    }

    public async Task<AdministratorSession?> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        AdministratorSessionDocument? existing;
        AdministratorAccountDocument? account;
        await using (var query = sessions.QuerySession())
        {
            existing = await query.FindSessionByTokenAsync(sessionToken, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            account = await query.LoadAsync<AdministratorAccountDocument>(existing.AccountId, cancellationToken);
        }

        if (existing.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            await RevokeAsync(existing, cancellationToken);
            return null;
        }

        return account is null
            ? null
            : new AdministratorSession(account.Id, account.UserName, account.Role, existing.ExpiresAtUtc, string.Empty);
    }

    public async Task<AdministratorSession?> RenewSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        await using var session = sessions.OpenSession();
        var existing = await session.FindSessionByTokenAsync(sessionToken, cancellationToken);
        if (existing is null || existing.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            return null;
        }

        var account = await session.LoadAsync<AdministratorAccountDocument>(existing.AccountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var oldSession = await session.Events.FetchForWriting<AdministratorSessionDocument>(existing.Id, cancellationToken);
        if (oldSession.Aggregate is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        oldSession.AppendOne(new SessionRevoked(existing.Id, account.Id, now));
        var newToken = StartSession(session, account.Id, now, out var expiresAtUtc);

        // A concurrent renewal or logout of the same session wins; this one is rejected.
        return await session.TrySaveChangesAsync(cancellationToken)
            ? new AdministratorSession(account.Id, account.UserName, account.Role, expiresAtUtc, newToken)
            : null;
    }

    public async Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        AdministratorSessionDocument? existing;
        await using (var query = sessions.QuerySession())
        {
            existing = await query.FindSessionByTokenAsync(sessionToken, cancellationToken);
        }

        if (existing is not null)
        {
            await RevokeAsync(existing, cancellationToken);
        }
    }

    private static string StartSession(IDocumentSession session, Guid accountId, DateTimeOffset now, out DateTimeOffset expiresAtUtc)
    {
        var sessionId = Guid.NewGuid();
        var token = AdministratorSecrets.CreateRandomToken();
        expiresAtUtc = now.Add(SessionLifetime);
        session.Events.StartStream<AdministratorSessionDocument>(
            sessionId,
            new SessionCreated(sessionId, AdministratorSecrets.HashSecret(token), accountId, expiresAtUtc, now));
        return token;
    }

    private async Task RevokeAsync(AdministratorSessionDocument existing, CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession(existing.AccountId.ToString("D"));
        var stream = await session.Events.FetchForWriting<AdministratorSessionDocument>(existing.Id, cancellationToken);
        if (stream.Aggregate is null)
        {
            return;
        }

        stream.AppendOne(new SessionRevoked(existing.Id, existing.AccountId, timeProvider.GetUtcNow()));

        // A concurrent revocation of the same session already achieved the same outcome.
        await session.TrySaveChangesAsync(cancellationToken);
    }
}
