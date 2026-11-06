using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using Marten;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStore");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IPasswordHasher<object>, PasswordHasher<object>>();
        services.AddSingleton<IAdministratorAuthenticationService, AdministratorAuthenticationService>();
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

        services.TryAddSingleton<IEventActorContext, BackgroundEventActorContext>();
        services.AddSingleton<IEventStreamStore>(provider => new AuthenticatedActorEventStreamStore(
            new MartenEventStreamStore(
                provider.GetRequiredService<IDocumentStore>(),
                provider.GetRequiredService<EventUpcasterRegistry>()),
            provider.GetRequiredService<IEventActorContext>()));
        services.AddHealthChecks()
            .AddCheck<EventStoreReadinessHealthCheck>("event-store", tags: ["ready"]);

        return services;
    }
}
