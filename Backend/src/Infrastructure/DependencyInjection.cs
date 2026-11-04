using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStore");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddHostedService<EventStoreSchemaInitializer>();
        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.AutoCreateSchemaObjects = AutoCreate.None;
            options.Events.StreamIdentity = StreamIdentity.AsGuid;
            options.Events.MetadataConfig.EnableAll();
            options.Events.MetadataConfig.UserNameEnabled = true;
            options.Events.AddEventType<PersistedDomainEvent>();
            options.Schema.For<CommandReceiptDocument>();
            options.Schema.For<GlobalEventIdDocument>();
        }).AddAsyncDaemon(DaemonMode.Solo);

        // Register an empty upcaster registry by default. Feature modules add upcasters by
        // registering IEventUpcaster implementations before the host starts.
        services.AddSingleton<EventUpcasterRegistry>(
            provider => new EventUpcasterRegistry(provider.GetServices<IEventUpcaster>()));

        services.AddSingleton<IEventStreamStore, MartenEventStreamStore>();
        services.AddHealthChecks()
            .AddCheck<EventStoreReadinessHealthCheck>("event-store", tags: ["ready"]);

        return services;
    }
}
