using Marten;
using Marten.Exceptions;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Skills;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

/// <summary>
/// Network commands follow Marten's command handler workflow: <c>FetchForWriting</c> on every stream the decision
/// depends on, then one <c>SaveChangesAsync</c>. Every default change appends to the one network-default stream,
/// so concurrent default changes conflict on its version; a default change and a disable of the same network both
/// append to that network's stream, so its version check rejects the later one.
/// </summary>
internal sealed class NetworkService(AttributedSessionFactory sessions, TimeProvider timeProvider) : INetworkService
{
    public async Task<IReadOnlyList<NetworkSummary>> GetNetworksAsync(bool includeDisabled,
        CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        var networks = includeDisabled
            ? await query.Query<NetworkDocument>().ToListAsync(cancellationToken)
            : await query.Query<NetworkDocument>().Where(network => network.IsEnabled).ToListAsync(cancellationToken);
        return networks
            .OrderBy(network => network.NormalizedName, StringComparer.Ordinal)
            .Select(network => network.ToSummary())
            .ToArray();
    }

    public async Task<NetworkSummary?> GetNetworkAsync(Guid networkId, CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return (await query.LoadAsync<NetworkDocument>(networkId, cancellationToken))?.ToSummary();
    }

    public async Task<NetworkResult> CreateNetworkAsync(string code, NetworkDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (NetworkDefinitionValidator.ValidateCreation(code, definition) is { } invalid)
        {
            return NetworkResult.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var normalizedCode = NetworkCodes.Normalize(code);
        if (await IsCodeUsedAsync(session, normalizedCode, cancellationToken))
        {
            return NetworkResult.Failure(NetworkOutcome.CodeAlreadyUsed);
        }

        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, exceptNetworkId: null, cancellationToken))
        {
            return NetworkResult.Failure(NetworkOutcome.NameAlreadyUsed);
        }

        var networkId = Guid.NewGuid();
        session.Events.StartStream<NetworkDocument>(
            networkId,
            new NetworkCreated(
                networkId,
                normalizedCode,
                definition.Name.Trim(),
                normalizedName,
                CatalogTexts.Normalize(definition.Description),
                definition.Theme,
                timeProvider.GetUtcNow()));
        return await session.SaveNetworkAsync(networkId, () => CreateConflictAsync(normalizedCode, cancellationToken),
            cancellationToken);
    }

    public async Task<NetworkResult> UpdateNetworkAsync(
        Guid networkId,
        long expectedVersion,
        NetworkDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (NetworkDefinitionValidator.Validate(definition) is { } invalid)
        {
            return NetworkResult.Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedNetworkAsync(networkId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return NetworkResult.Failure(outcome);
        }

        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, networkId, cancellationToken))
        {
            return NetworkResult.Failure(NetworkOutcome.NameAlreadyUsed);
        }

        var name = definition.Name.Trim();
        var description = CatalogTexts.Normalize(definition.Description);
        if (current.Name == name && current.Description == description && current.Theme == definition.Theme)
        {
            return NetworkResult.Success(current);
        }

        stream.AppendOne(new NetworkDetailsChanged(networkId, name, normalizedName, description, definition.Theme,
            timeProvider.GetUtcNow()));
        return await session.SaveNetworkAsync(networkId, NetworkWrites.NameConflict, cancellationToken);
    }

    public async Task<NetworkResult> EnableNetworkAsync(Guid networkId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<NetworkDocument>(networkId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return NetworkResult.Failure(NetworkOutcome.NotFound);
            case { IsEnabled: true } current:
                return NetworkResult.Success(current);
            default:
                stream.AppendOne(new NetworkEnabled(networkId, timeProvider.GetUtcNow()));
                return await session.SaveNetworkAsync(networkId, NetworkWrites.NameConflict, cancellationToken);
        }
    }

    public async Task<NetworkResult> DisableNetworkAsync(Guid networkId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<NetworkDocument>(networkId, cancellationToken);
        switch (stream.Aggregate)
        {
            case null:
                return NetworkResult.Failure(NetworkOutcome.NotFound);
            case { IsDefault: true }:
                return NetworkResult.Failure(NetworkOutcome.DefaultNetworkCannotBeDisabled);
            case { IsEnabled: false } current:
                return NetworkResult.Success(current);
            default:
                stream.AppendOne(new NetworkDisabled(networkId, timeProvider.GetUtcNow()));
                return await session.SaveNetworkAsync(networkId, NetworkWrites.NameConflict, cancellationToken);
        }
    }

    public async Task<NetworkResult> MakeDefaultNetworkAsync(Guid networkId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();

        // Take the network-default stream before reading which network is the default: a default change committed
        // before this fetch is then visible to the query below, and one committed after it fails this command's
        // version check on that stream.
        var defaultStream =
            await session.Events.FetchForWriting<NetworkDefaultDocument>(NetworkDefaultDocument.StreamId,
                cancellationToken);
        var previousDefaultIds = await session.Query<NetworkDocument>()
            .Where(network => network.IsDefault && network.Id != networkId)
            .Select(network => network.Id)
            .ToListAsync(cancellationToken);
        Guid[] streamIds = [NetworkDefaultDocument.StreamId, networkId, .. previousDefaultIds];
        var streams = await session.Events.FetchManyForWriting<NetworkDocument>(streamIds[1..], cancellationToken);
        var target = streams[0];
        switch (target.Aggregate)
        {
            case null:
                return NetworkResult.Failure(NetworkOutcome.NotFound);
            case { IsEnabled: false }:
                return NetworkResult.Failure(NetworkOutcome.NetworkDisabled);
            case { IsDefault: true } current:
                return NetworkResult.Success(current);
        }

        var now = timeProvider.GetUtcNow();
        defaultStream.AppendOne(new DefaultNetworkChosen(networkId, now));
        target.AppendOne(new NetworkMadeDefault(networkId, now));
        for (var index = 1; index < streams.Count; index++)
        {
            if (streams[index].Aggregate is { IsDefault: true })
            {
                streams[index].AppendOne(new NetworkDefaultCleared(streamIds[index + 1], networkId, now));
            }
        }

        try
        {
            return await session.SaveNetworkAsync(networkId, NetworkWrites.NameConflict, cancellationToken);
        }
        catch (ExistingStreamIdCollisionException)
        {
            // Two first-ever default changes both started the network-default stream; the later one lost.
            return NetworkResult.Failure(NetworkOutcome.StaleVersion);
        }
    }

    private async Task<NetworkOutcome> CreateConflictAsync(string normalizedCode, CancellationToken cancellationToken)
    {
        await using var query = sessions.QuerySession();
        return await IsCodeUsedAsync(query, normalizedCode, cancellationToken)
            ? NetworkOutcome.CodeAlreadyUsed
            : NetworkOutcome.NameAlreadyUsed;
    }

    private static Task<bool> IsCodeUsedAsync(IQuerySession session, string normalizedCode,
        CancellationToken cancellationToken) =>
        session.Query<NetworkDocument>().AnyAsync(network => network.Code == normalizedCode, cancellationToken);

    private static Task<bool> IsNameUsedAsync(
        IQuerySession session,
        string normalizedName,
        Guid? exceptNetworkId,
        CancellationToken cancellationToken)
    {
        var sameName = session.Query<NetworkDocument>().Where(network => network.NormalizedName == normalizedName);
        return exceptNetworkId is { } excluded
            ? sameName.Where(network => network.Id != excluded).AnyAsync(cancellationToken)
            : sameName.AnyAsync(cancellationToken);
    }
}
