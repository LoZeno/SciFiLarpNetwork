using JasperFx.Events;
using JasperFx.Events.Daemon;
using Marten;
using Marten.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Domain.Events;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using Weasel.Postgresql.Tables.Partitioning;

namespace SciFiLarpNetwork.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStore");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<object>, PasswordHasher<object>>();
        services.AddSingleton<IAdministratorAuthenticationService, AdministratorAuthenticationService>();
        services.AddHostedService<EventStoreSchemaInitializer>();
        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.Events.StreamIdentity = StreamIdentity.AsGuid;
            options.Events.MetadataConfig.EnableAll();
            options.Events.MetadataConfig.UserNameEnabled = true;
            options.Events.AppendMode = EventAppendMode.Rich;

            options.Events.AddEventType<AccountCreated>();
            options.Events.AddEventType<AccountPasswordChanged>();
            options.Events.AddEventType<AccountRoleChanged>();
            options.Events.AddEventType<BootstrapCredentialIssued>();
            options.Events.AddEventType<RecoveryCredentialIssued>();
            options.Events.AddEventType<GrantConsumed>();
            options.Events.AddEventType<SessionCreated>();
            options.Events.AddEventType<SessionRevoked>();
            options.Events.AddEventType<AuditEventLogged>();

            // Administrator accounts, sessions and grants are write models: sign-in, session
            // validation and grant checks must see every committed event immediately. Inline
            // projections are updated in the same transaction as the appended events, which
            // gives that strong consistency (Marten "Projections" docs, ProjectionLifecycle).
            options.Projections.Add<AdministratorAccountProjection>(JasperFx.Events.Projections.ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorSessionProjection>(JasperFx.Events.Projections.ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorGrantProjection>(JasperFx.Events.Projections.ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorAuditProjection>(JasperFx.Events.Projections.ProjectionLifecycle.Inline);
        }).AddAsyncDaemon(DaemonMode.Solo);

        services.TryAddSingleton<IEventActorContext, BackgroundEventActorContext>();
        services.AddHealthChecks()
            .AddCheck<EventStoreReadinessHealthCheck>("event-store", tags: ["ready"]);

        return services;
    }
}
