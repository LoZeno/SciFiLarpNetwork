using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CardFlagEvents;
using SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;
using SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Skills.SkillDocuments;
using Weasel.Core;

namespace SciFiLarpNetwork.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStore");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<object>, PasswordHasher<object>>();
        services.AddSingleton<AttributedSessionFactory>();
        services.AddSingleton<IAdministratorProvisioningService, AdministratorProvisioningService>();
        services.AddSingleton<IAdministratorSessionService, AdministratorSessionService>();
        services.AddSingleton<IAdministratorAccountService, AdministratorAccountService>();
        services.AddSingleton<ITalentService, TalentService>();
        services.AddSingleton<ISkillService, SkillService>();
        services.AddSingleton<ICardFlagService, CardFlagService>();
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
            options.Projections.Add<AdministratorAccountProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorSessionProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorGrantProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorAuditProjection>(ProjectionLifecycle.Inline);

            options.Schema.For<AdministratorAccountDocument>()
                .UniqueIndex(UniqueIndexType.Computed, account => account.NormalizedUserName);

            // Game flags are persisted by name so stored events stay readable if enum members are reordered.
            options.UseSystemTextJsonForSerialization(enumStorage: EnumStorage.AsString);

            options.Events.AddEventType<TalentCreated>();
            options.Events.AddEventType<TalentDetailsChanged>();
            options.Events.AddEventType<TalentArchived>();
            options.Events.AddEventType<TalentUnarchived>();
            options.Events.AddEventType<SkillCreated>();
            options.Events.AddEventType<SkillDetailsChanged>();
            options.Events.AddEventType<SkillTalentsChanged>();
            options.Events.AddEventType<SkillGameFlagsChanged>();
            options.Events.AddEventType<SkillCardFlagsChanged>();
            options.Events.AddEventType<SkillPrerequisitesChanged>();
            options.Events.AddEventType<SkillArchived>();
            options.Events.AddEventType<SkillUnarchived>();
            options.Events.AddEventType<CardFlagDefined>();

            // Skill commands decide on these write models (unique names, archived talents, prerequisite loops),
            // so they are updated in the same transaction as the events.
            options.Projections.Add<TalentProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<SkillProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<CardFlagProjection>(ProjectionLifecycle.Inline);

            options.Schema.For<TalentDocument>()
                .UniqueIndex(UniqueIndexType.Computed, talent => talent.NormalizedName);
            options.Schema.For<SkillDocument>()
                .UniqueIndex(UniqueIndexType.Computed, skill => skill.NormalizedName);
            options.Schema.For<CardFlagDocument>()
                .UniqueIndex(UniqueIndexType.Computed, cardFlag => cardFlag.NormalizedName);
        }).AddAsyncDaemon(DaemonMode.Solo);

        services.TryAddSingleton<IEventActorContext, BackgroundEventActorContext>();
        services.AddHealthChecks()
            .AddCheck<EventStoreReadinessHealthCheck>("event-store", tags: ["ready"]);

        return services;
    }
}
