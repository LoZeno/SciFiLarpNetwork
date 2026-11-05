using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.Administration;

namespace SciFiLarpNetwork.Backend.Infrastructure.Administration;

public sealed class AdministratorAuthenticationService(
    NpgsqlDataSource dataSource,
    IPasswordHasher<object> passwordHasher) : IAdministratorAuthenticationService, IDisposable
{
    private static readonly TimeSpan CredentialLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private const long AdministrationLockId = 0x53464C4E41555448;
    private const string AccountTable = "scifi_administrator_accounts";
    private const string GrantTable = "scifi_administrator_grants";
    private const string SessionTable = "scifi_administrator_sessions";
    private const string AuditTable = "scifi_administrator_audit_events";
    private static readonly Regex UserNamePattern = new("^[\\p{L}\\p{N}][\\p{L}\\p{N} ._@+-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly SemaphoreSlim _schemaInitializationLock = new(1, 1);
    private readonly object _passwordSubject = new();
    private readonly string _dummyPasswordHash = passwordHasher.HashPassword(new object(), CreateRandomToken());
    private bool _schemaInitialized;

    public void Dispose() => _schemaInitializationLock.Dispose();

    public async Task<string> IssueBootstrapCredentialAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var credential = CreateRandomToken();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);

        if (await CountAccountsAsync(connection, transaction, cancellationToken) != 0)
        {
            throw new InvalidOperationException("Administrator bootstrap has already been completed.");
        }

        await using var command = new NpgsqlCommand(
            $"INSERT INTO {GrantTable} (id, purpose, token_hash, expires_at_utc) " +
            "VALUES (1, 'bootstrap', @tokenHash, @expiresAtUtc) " +
            "ON CONFLICT (id) DO UPDATE SET purpose = EXCLUDED.purpose, " +
            "token_hash = EXCLUDED.token_hash, expires_at_utc = EXCLUDED.expires_at_utc",
            connection,
            transaction);
        command.Parameters.AddWithValue("tokenHash", HashSecret(credential));
        command.Parameters.AddWithValue("expiresAtUtc", DateTime.UtcNow.Add(CredentialLifetime));
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return credential;
    }

    public async Task<string> IssueRecoveryCredentialAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var credential = CreateRandomToken();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);

        if (await CountAccountsAsync(connection, transaction, cancellationToken) == 0)
        {
            throw new InvalidOperationException("There are no administrator accounts to recover.");
        }

        await using var command = new NpgsqlCommand(
            $"INSERT INTO {GrantTable} (id, purpose, token_hash, expires_at_utc) " +
            "VALUES (1, 'recovery', @tokenHash, @expiresAtUtc) " +
            "ON CONFLICT (id) DO UPDATE SET purpose = EXCLUDED.purpose, " +
            "token_hash = EXCLUDED.token_hash, expires_at_utc = EXCLUDED.expires_at_utc",
            connection,
            transaction);
        command.Parameters.AddWithValue("tokenHash", HashSecret(credential));
        command.Parameters.AddWithValue("expiresAtUtc", DateTime.UtcNow.Add(CredentialLifetime));
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return credential;
    }

    public async Task<AdministratorOperationResult> BootstrapAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCredentials(userName, password))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);

        var grant = await ReadGrantAsync(connection, transaction, cancellationToken);
        if (!IsValidGrant(grant, credential, "bootstrap"))
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.InvalidGrant;
        }

        if (await CountAccountsAsync(connection, transaction, cancellationToken) != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.AlreadyInitialized;
        }

        var accountId = Guid.NewGuid();
        var normalizedUserName = NormalizeUserName(userName);
        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
        try
        {
            await using var insert = new NpgsqlCommand(
                $"INSERT INTO {AccountTable} (id, user_name, normalized_user_name, password_hash, role, created_at_utc) " +
                "VALUES (@id, @userName, @normalizedUserName, @passwordHash, @role, @createdAtUtc)",
                connection,
                transaction);
            insert.Parameters.AddWithValue("id", accountId);
            insert.Parameters.AddWithValue("userName", userName.Trim());
            insert.Parameters.AddWithValue("normalizedUserName", normalizedUserName);
            insert.Parameters.AddWithValue("passwordHash", passwordHash);
            insert.Parameters.AddWithValue("role", AdministratorRoles.Administrator);
            insert.Parameters.AddWithValue("createdAtUtc", DateTime.UtcNow);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.AccountNameAlreadyUsed;
        }

        await using (var audit = new NpgsqlCommand(
                         $"INSERT INTO {AuditTable} (id, actor_kind, actor_id, target_account_id, event_type, " +
                         "previous_role, new_role, occurred_at_utc) " +
                         "VALUES (@id, 'local-operator', NULL, @targetId, 'AdministratorBootstrapped', " +
                         "'none', @newRole, @occurredAtUtc)",
                         connection,
                         transaction))
        {
            audit.Parameters.AddWithValue("id", Guid.NewGuid());
            audit.Parameters.AddWithValue("targetId", accountId);
            audit.Parameters.AddWithValue("newRole", AdministratorRoles.Administrator);
            audit.Parameters.AddWithValue("occurredAtUtc", DateTime.UtcNow);
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        await DeleteGrantAsync(connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AdministratorOperationResult.Succeeded;
    }

    public async Task<AdministratorOperationResult> RecoverAsync(
        string credential,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCredentials(userName, password))
        {
            return AdministratorOperationResult.InvalidGrant;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);

        var grant = await ReadGrantAsync(connection, transaction, cancellationToken);
        if (!IsValidGrant(grant, credential, "recovery"))
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.InvalidGrant;
        }

        var normalizedUserName = NormalizeUserName(userName);
        Guid accountId;
        await using (var find = new NpgsqlCommand(
                         $"SELECT id FROM {AccountTable} WHERE normalized_user_name = @normalizedUserName FOR UPDATE",
                         connection,
                         transaction))
        {
            find.Parameters.AddWithValue("normalizedUserName", normalizedUserName);
            var result = await find.ExecuteScalarAsync(cancellationToken);
            if (result is not Guid foundAccountId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AdministratorOperationResult.AccountNotFound;
            }

            accountId = foundAccountId;
        }

        var passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
        await using (var update = new NpgsqlCommand(
                         $"UPDATE {AccountTable} SET password_hash = @passwordHash WHERE id = @accountId; " +
                         $"DELETE FROM {SessionTable} WHERE account_id = @accountId",
                         connection,
                         transaction))
        {
            update.Parameters.AddWithValue("passwordHash", passwordHash);
            update.Parameters.AddWithValue("accountId", accountId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var audit = new NpgsqlCommand(
                         $"INSERT INTO {AuditTable} (id, actor_kind, actor_id, target_account_id, event_type, " +
                         "previous_role, new_role, occurred_at_utc) " +
                         "SELECT @id, 'local-operator', NULL, id, 'AdministratorCredentialRecovered', role, role, @occurredAtUtc " +
                         $"FROM {AccountTable} WHERE id = @accountId",
                         connection,
                         transaction))
        {
            audit.Parameters.AddWithValue("id", Guid.NewGuid());
            audit.Parameters.AddWithValue("accountId", accountId);
            audit.Parameters.AddWithValue("occurredAtUtc", DateTime.UtcNow);
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        await DeleteGrantAsync(connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AdministratorOperationResult.Succeeded;
    }

    public async Task<AdministratorSession?> SignInAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var normalizedUserName = NormalizeUserName(userName);
        Guid accountId = Guid.Empty;
        string storedUserName = string.Empty;
        string passwordHash = _dummyPasswordHash;
        string role = string.Empty;

        await using (var find = new NpgsqlCommand(
                         $"SELECT id, user_name, password_hash, role FROM {AccountTable} " +
                         "WHERE normalized_user_name = @normalizedUserName FOR UPDATE",
                         connection,
                         transaction))
        {
            find.Parameters.AddWithValue("normalizedUserName", normalizedUserName);
            await using var reader = await find.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                accountId = reader.GetGuid(0);
                storedUserName = reader.GetString(1);
                passwordHash = reader.GetString(2);
                role = reader.GetString(3);
            }
        }

        var verification = passwordHasher.VerifyHashedPassword(_passwordSubject, passwordHash, password);
        if (accountId == Guid.Empty || verification == PasswordVerificationResult.Failed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            passwordHash = passwordHasher.HashPassword(_passwordSubject, password);
            await using var update = new NpgsqlCommand(
                $"UPDATE {AccountTable} SET password_hash = @passwordHash WHERE id = @accountId",
                connection,
                transaction);
            update.Parameters.AddWithValue("passwordHash", passwordHash);
            update.Parameters.AddWithValue("accountId", accountId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        var token = CreateRandomToken();
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);
        await using (var insert = new NpgsqlCommand(
                         $"INSERT INTO {SessionTable} (token_hash, account_id, expires_at_utc) " +
                         "VALUES (@tokenHash, @accountId, @expiresAtUtc)",
                         connection,
                         transaction))
        {
            insert.Parameters.AddWithValue("tokenHash", HashSecret(token));
            insert.Parameters.AddWithValue("accountId", accountId);
            insert.Parameters.AddWithValue("expiresAtUtc", expiresAtUtc.UtcDateTime);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdministratorSession(accountId, storedUserName, role, expiresAtUtc, token);
    }

    public async Task<AdministratorSession?> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT account.id, account.user_name, account.role, s.expires_at_utc " +
            $"FROM {SessionTable} AS s INNER JOIN {AccountTable} AS account " +
            "ON account.id = s.account_id WHERE s.token_hash = @tokenHash",
            connection);
        command.Parameters.AddWithValue("tokenHash", HashSecret(sessionToken));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var expiresAtUtc = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc));
        if (expiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await reader.DisposeAsync();
            await using var delete = new NpgsqlCommand(
                $"DELETE FROM {SessionTable} WHERE token_hash = @tokenHash",
                connection);
            delete.Parameters.AddWithValue("tokenHash", HashSecret(sessionToken));
            await delete.ExecuteNonQueryAsync(cancellationToken);
            return null;
        }

        return new AdministratorSession(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), expiresAtUtc, string.Empty);
    }

    public async Task<AdministratorSession?> RenewSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var oldTokenHash = HashSecret(sessionToken);
        Guid accountId;
        string userName;
        string role;
        DateTimeOffset oldExpiresAtUtc;
        await using (var find = new NpgsqlCommand(
                         $"SELECT account.id, account.user_name, account.role, s.expires_at_utc " +
                         $"FROM {SessionTable} AS s INNER JOIN {AccountTable} AS account " +
                         "ON account.id = s.account_id WHERE s.token_hash = @tokenHash FOR UPDATE OF s",
                         connection,
                         transaction))
        {
            find.Parameters.AddWithValue("tokenHash", oldTokenHash);
            await using var reader = await find.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            accountId = reader.GetGuid(0);
            userName = reader.GetString(1);
            role = reader.GetString(2);
            oldExpiresAtUtc = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc));
        }

        if (oldExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var newToken = CreateRandomToken();
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);
        await using (var delete = new NpgsqlCommand(
                         $"DELETE FROM {SessionTable} WHERE token_hash = @tokenHash",
                         connection,
                         transaction))
        {
            delete.Parameters.AddWithValue("tokenHash", oldTokenHash);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insert = new NpgsqlCommand(
                         $"INSERT INTO {SessionTable} (token_hash, account_id, expires_at_utc) " +
                         "VALUES (@tokenHash, @accountId, @expiresAtUtc)",
                         connection,
                         transaction))
        {
            insert.Parameters.AddWithValue("tokenHash", HashSecret(newToken));
            insert.Parameters.AddWithValue("accountId", accountId);
            insert.Parameters.AddWithValue("expiresAtUtc", expiresAtUtc.UtcDateTime);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdministratorSession(accountId, userName, role, expiresAtUtc, newToken);
    }

    public async Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"DELETE FROM {SessionTable} WHERE token_hash = @tokenHash",
            connection);
        command.Parameters.AddWithValue("tokenHash", HashSecret(sessionToken));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdministratorSummary>> GetAdministratorsAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT id, user_name, role, created_at_utc FROM {AccountTable} ORDER BY normalized_user_name",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var accounts = new List<AdministratorSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            accounts.Add(new AdministratorSummary(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc))));
        }

        return accounts;
    }

    public async Task<AdministratorAccountCreationResult> CreateAccountAsync(
        Guid actorAccountId,
        string userName,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCredentials(userName, password) ||
            role is not (AdministratorRoles.Reader or AdministratorRoles.Administrator))
        {
            return new AdministratorAccountCreationResult(AdministratorOperationResult.InvalidGrant, null);
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);
        if (await ReadRoleAsync(connection, transaction, actorAccountId, cancellationToken) != AdministratorRoles.Administrator)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AdministratorAccountCreationResult(AdministratorOperationResult.ActorNoLongerAuthorized, null);
        }

        var accountId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;
        await using (var insert = new NpgsqlCommand(
                         $"INSERT INTO {AccountTable} (id, user_name, normalized_user_name, password_hash, role, created_at_utc) " +
                         "VALUES (@id, @userName, @normalizedUserName, @passwordHash, @role, @createdAtUtc)",
                         connection,
                         transaction))
        {
            insert.Parameters.AddWithValue("id", accountId);
            insert.Parameters.AddWithValue("userName", userName.Trim());
            insert.Parameters.AddWithValue("normalizedUserName", NormalizeUserName(userName));
            insert.Parameters.AddWithValue("passwordHash", passwordHasher.HashPassword(_passwordSubject, password));
            insert.Parameters.AddWithValue("role", role);
            insert.Parameters.AddWithValue("createdAtUtc", createdAtUtc);
            try
            {
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AdministratorAccountCreationResult(AdministratorOperationResult.AccountNameAlreadyUsed, null);
            }
        }

        await using (var audit = new NpgsqlCommand(
                         $"INSERT INTO {AuditTable} (id, actor_kind, actor_id, target_account_id, event_type, " +
                         "previous_role, new_role, occurred_at_utc) " +
                         "VALUES (@id, 'administrator', @actorId, @targetId, 'AdministratorAccountCreated', " +
                         "'none', @newRole, @occurredAtUtc)",
                         connection,
                         transaction))
        {
            audit.Parameters.AddWithValue("id", Guid.NewGuid());
            audit.Parameters.AddWithValue("actorId", actorAccountId);
            audit.Parameters.AddWithValue("targetId", accountId);
            audit.Parameters.AddWithValue("newRole", role);
            audit.Parameters.AddWithValue("occurredAtUtc", createdAtUtc);
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdministratorAccountCreationResult(
            AdministratorOperationResult.Succeeded,
            new AdministratorSummary(accountId, userName.Trim(), role, new DateTimeOffset(createdAtUtc, TimeSpan.Zero)));
    }

    public async Task<AdministratorOperationResult> ChangeRoleAsync(
        Guid actorAccountId,
        Guid targetAccountId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (AdministratorRoles.Reader or AdministratorRoles.Administrator))
        {
            return AdministratorOperationResult.AccountNotFound;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);

        var actorRole = await ReadRoleAsync(connection, transaction, actorAccountId, cancellationToken);
        if (actorRole != AdministratorRoles.Administrator)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.ActorNoLongerAuthorized;
        }

        string? previousRole;
        await using (var find = new NpgsqlCommand(
                         $"SELECT role FROM {AccountTable} WHERE id = @accountId FOR UPDATE",
                         connection,
                         transaction))
        {
            find.Parameters.AddWithValue("accountId", targetAccountId);
            previousRole = await find.ExecuteScalarAsync(cancellationToken) as string;
        }

        if (previousRole is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.AccountNotFound;
        }

        if (previousRole == role)
        {
            await transaction.CommitAsync(cancellationToken);
            return AdministratorOperationResult.Succeeded;
        }

        if (previousRole == AdministratorRoles.Administrator && role == AdministratorRoles.Reader &&
            await CountAdministratorsAsync(connection, transaction, cancellationToken) <= 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AdministratorOperationResult.LastAdministrator;
        }

        var occurredAtUtc = DateTime.UtcNow;
        await using (var update = new NpgsqlCommand(
                         $"UPDATE {AccountTable} SET role = @role WHERE id = @accountId",
                         connection,
                         transaction))
        {
            update.Parameters.AddWithValue("role", role);
            update.Parameters.AddWithValue("accountId", targetAccountId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var audit = new NpgsqlCommand(
                         $"INSERT INTO {AuditTable} (id, actor_kind, actor_id, target_account_id, event_type, " +
                         "previous_role, new_role, occurred_at_utc) " +
                         "VALUES (@id, 'administrator', @actorId, @targetId, 'AdministratorRoleChanged', " +
                         "@previousRole, @newRole, @occurredAtUtc)",
                         connection,
                         transaction))
        {
            audit.Parameters.AddWithValue("id", Guid.NewGuid());
            audit.Parameters.AddWithValue("actorId", actorAccountId);
            audit.Parameters.AddWithValue("targetId", targetAccountId);
            audit.Parameters.AddWithValue("previousRole", previousRole);
            audit.Parameters.AddWithValue("newRole", role);
            audit.Parameters.AddWithValue("occurredAtUtc", occurredAtUtc);
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return AdministratorOperationResult.Succeeded;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaInitialized)
        {
            return;
        }

        await _schemaInitializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_schemaInitialized)
            {
                return;
            }

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await AcquireAdministrationLockAsync(connection, transaction, cancellationToken);
            await using var command = new NpgsqlCommand(
                $"CREATE TABLE IF NOT EXISTS {AccountTable} (" +
                "id uuid PRIMARY KEY, user_name text NOT NULL, normalized_user_name text NOT NULL, " +
                "password_hash text NOT NULL, role text NOT NULL CHECK (role IN ('reader', 'administrator')), " +
                "created_at_utc timestamptz NOT NULL, " +
                "CONSTRAINT ux_scifi_administrator_accounts_normalized_user_name UNIQUE (normalized_user_name)); " +
                $"CREATE TABLE IF NOT EXISTS {GrantTable} (" +
                "id smallint PRIMARY KEY CHECK (id = 1), purpose text NOT NULL CHECK (purpose IN ('bootstrap', 'recovery')), " +
                "token_hash text NOT NULL, expires_at_utc timestamptz NOT NULL); " +
                $"CREATE TABLE IF NOT EXISTS {SessionTable} (" +
                "token_hash text PRIMARY KEY, account_id uuid NOT NULL REFERENCES " + AccountTable + " (id) ON DELETE CASCADE, " +
                "expires_at_utc timestamptz NOT NULL); " +
                $"CREATE INDEX IF NOT EXISTS ix_scifi_administrator_sessions_account_id ON {SessionTable} (account_id); " +
                $"CREATE TABLE IF NOT EXISTS {AuditTable} (" +
                "id uuid PRIMARY KEY, actor_kind text NOT NULL, actor_id uuid NULL REFERENCES " + AccountTable + " (id), " +
                "target_account_id uuid NOT NULL REFERENCES " + AccountTable + " (id), event_type text NOT NULL, " +
                "previous_role text NOT NULL, new_role text NOT NULL, occurred_at_utc timestamptz NOT NULL)",
                connection,
                transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _schemaInitialized = true;
        }
        finally
        {
            _schemaInitializationLock.Release();
        }
    }

    private static async Task AcquireAdministrationLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@lockId)", connection, transaction);
        command.Parameters.AddWithValue("lockId", AdministrationLockId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> CountAccountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {AccountTable}", connection, transaction);
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    private static async Task<long> CountAdministratorsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM {AccountTable} WHERE role = @role",
            connection,
            transaction);
        command.Parameters.AddWithValue("role", AdministratorRoles.Administrator);
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    private static async Task<string?> ReadRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT role FROM {AccountTable} WHERE id = @accountId",
            connection,
            transaction);
        command.Parameters.AddWithValue("accountId", accountId);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private static async Task<CredentialGrant?> ReadGrantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT purpose, token_hash, expires_at_utc FROM {GrantTable} WHERE id = 1 FOR UPDATE",
            connection,
            transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new CredentialGrant(
                reader.GetString(0),
                reader.GetString(1),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)))
            : null;
    }

    private static bool IsValidGrant(CredentialGrant? grant, string credential, string purpose)
    {
        if (grant is null || grant.Purpose != purpose || grant.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
            string.IsNullOrWhiteSpace(credential))
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(grant.TokenHash),
                Convert.FromHexString(HashSecret(credential)));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static async Task DeleteGrantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"DELETE FROM {GrantTable} WHERE id = 1", connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsValidCredentials(string userName, string password) =>
        !string.IsNullOrWhiteSpace(userName) &&
        UserNamePattern.IsMatch(userName.Trim()) &&
        !string.IsNullOrWhiteSpace(password) &&
        password.Length is >= 12 and <= 128;

    private static string NormalizeUserName(string userName) =>
        (userName ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    private static string CreateRandomToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private sealed record CredentialGrant(string Purpose, string TokenHash, DateTimeOffset ExpiresAtUtc);
}