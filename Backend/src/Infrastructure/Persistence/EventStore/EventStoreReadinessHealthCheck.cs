using Marten;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class EventStoreReadinessHealthCheck(IDocumentStore documentStore) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = documentStore.QuerySession();
            _ = await session.Query<CommandReceiptDocument>().CountAsync(cancellationToken);
            return HealthCheckResult.Healthy("The event store is available.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The event store is unavailable.", exception);
        }
    }
}
