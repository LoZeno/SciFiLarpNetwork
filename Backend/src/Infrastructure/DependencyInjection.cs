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
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Application.Skills;
using SciFiLarpNetwork.Backend.Domain.Events.AdministratorEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CardFlagEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CardTemplateEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;
using SciFiLarpNetwork.Backend.Domain.Events.RunningNumberEvents;
using SciFiLarpNetwork.Backend.Domain.Events.SkillEvents;
using SciFiLarpNetwork.Backend.Domain.Events.TalentEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.CardTemplates;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.Networks;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;
using SciFiLarpNetwork.Backend.Infrastructure.Players;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;
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
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<INetworkBackgroundImageService, NetworkBackgroundImageService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<ICharacterService, CharacterService>();
        services.AddSingleton<ICharacterStatService, CharacterStatService>();
        services.AddSingleton<ICharacterSkillService, CharacterSkillService>();
        services.AddSingleton<ICharacterCardService, CharacterCardService>();
        services.AddSingleton<ICharacterCardTemplateService, CharacterCardTemplateService>();
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
            options.Events.AddEventType<AccountAuthorityUsed>();
            options.Events.AddEventType<AdministratorRosterChanged>();

            // Administrator accounts, sessions and grants are write models: sign-in, session
            // validation and grant checks must see every committed event immediately. Inline
            // projections are updated in the same transaction as the appended events, which
            // gives that strong consistency (Marten "Projections" docs, ProjectionLifecycle).
            options.Projections.Add<AdministratorAccountProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorSessionProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorGrantProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorAuditProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<AdministratorRosterProjection>(ProjectionLifecycle.Inline);

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
            options.Events.AddEventType<PrerequisiteGraphChanged>();

            // Skill commands decide on these write models (unique names, archived talents, prerequisite loops),
            // so they are updated in the same transaction as the events.
            options.Projections.Add<TalentProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<SkillProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<CardFlagProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<PrerequisiteGraphProjection>(ProjectionLifecycle.Inline);

            options.Schema.For<TalentDocument>()
                .UniqueIndex(UniqueIndexType.Computed, talent => talent.NormalizedName);
            options.Schema.For<SkillDocument>()
                .UniqueIndex(UniqueIndexType.Computed, skill => skill.NormalizedName);
            options.Schema.For<CardFlagDocument>()
                .UniqueIndex(UniqueIndexType.Computed, cardFlag => cardFlag.NormalizedName);

            options.Events.AddEventType<NetworkCreated>();
            options.Events.AddEventType<NetworkDetailsChanged>();
            options.Events.AddEventType<NetworkBackgroundImageSet>();
            options.Events.AddEventType<NetworkBackgroundImageRemoved>();
            options.Events.AddEventType<NetworkEnabled>();
            options.Events.AddEventType<NetworkDisabled>();
            options.Events.AddEventType<NetworkMadeDefault>();
            options.Events.AddEventType<NetworkDefaultCleared>();
            options.Events.AddEventType<DefaultNetworkChosen>();

            // Network commands decide on these write models (unique names and codes, the single default).
            options.Projections.Add<NetworkProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<NetworkDefaultProjection>(ProjectionLifecycle.Inline);

            // Two calls create two separate unique indexes; one call with both members would make one composite index.
            options.Schema.For<NetworkDocument>()
                .UniqueIndex(UniqueIndexType.Computed, network => network.NormalizedName)
                .UniqueIndex(UniqueIndexType.Computed, network => network.Code);

            options.Events.AddEventType<RunningNumberIssued>();
            options.Events.AddEventType<PlayerRegistered>();
            options.Events.AddEventType<PlayerDetailsChanged>();
            options.Events.AddEventType<PlayerDeactivated>();
            options.Events.AddEventType<PlayerActivated>();
            options.Events.AddEventType<PlayerArchived>();
            options.Events.AddEventType<PlayerUnarchived>();
            options.Events.AddEventType<PlayerCharacterActivated>();
            options.Events.AddEventType<CharacterCreated>();
            options.Events.AddEventType<CharacterDetailsChanged>();
            options.Events.AddEventType<CharacterNetworkChanged>();
            options.Events.AddEventType<CharacterStatusChanged>();
            options.Events.AddEventType<CharacterStatValuesChanged>();
            options.Events.AddEventType<CharacterSkillsChanged>();
            options.Events.AddEventType<CharacterQrIdentifierRegenerated>();
            options.Events.AddEventType<CharacterArchived>();
            options.Events.AddEventType<CharacterUnarchived>();
            options.Events.AddEventType<CharacterStatDefined>();
            options.Events.AddEventType<CharacterStatRenamed>();
            options.Events.AddEventType<CharacterStatArchived>();
            options.Events.AddEventType<CharacterStatUnarchived>();

            // Player and character commands decide on these write models (running numbers, player status,
            // unique stat names and QR identifiers).
            options.Projections.Add<RunningNumberProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<PlayerProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<CharacterProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<CharacterStatProjection>(ProjectionLifecycle.Inline);

            options.Schema.For<PlayerDocument>()
                .UniqueIndex(UniqueIndexType.Computed, player => player.Number);
            options.Schema.For<CharacterDocument>()
                .UniqueIndex(UniqueIndexType.Computed, character => character.Number)
                .UniqueIndex(UniqueIndexType.Computed, character => character.QrIdentifier);
            options.Schema.For<CharacterStatDocument>()
                .UniqueIndex(UniqueIndexType.Computed, stat => stat.NormalizedName);

            options.Events.AddEventType<CardTemplateUploaded>();
            options.Events.AddEventType<CardTemplateReset>();

            // Template commands decide on the current template, and printing reads it right after a change.
            options.Projections.Add<CardTemplateProjection>(ProjectionLifecycle.Inline);
        }).AddAsyncDaemon(DaemonMode.Solo);

        services.TryAddSingleton<IEventActorContext, BackgroundEventActorContext>();
        services.AddHealthChecks()
            .AddCheck<EventStoreReadinessHealthCheck>("event-store", tags: ["ready"]);

        return services;
    }
}
