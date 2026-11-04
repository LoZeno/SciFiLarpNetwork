using Marten;
using Microsoft.Extensions.Hosting;

namespace SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

internal sealed class EventStoreSchemaInitializer(IDocumentStore documentStore) : IHostedService
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializationLock.WaitAsync(cancellationToken);
        try
        {
            await documentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
