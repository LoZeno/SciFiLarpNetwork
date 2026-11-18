using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

public partial class CharacterProjection : SingleStreamProjection<CharacterDocument, Guid>
{
    public CharacterProjection()
    {
        IncludeType<CharacterCreated>();
        IncludeType<CharacterDetailsChanged>();
        IncludeType<CharacterNetworkChanged>();
        IncludeType<CharacterStatusChanged>();
        IncludeType<CharacterStatValuesChanged>();
        IncludeType<CharacterSkillsChanged>();
        IncludeType<CharacterQrIdentifierRegenerated>();
        IncludeType<CharacterArchived>();
        IncludeType<CharacterUnarchived>();
    }

    public override CharacterDocument? Evolve(CharacterDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            CharacterCreated created => new CharacterDocument(
                created.CharacterId,
                created.Number,
                created.PlayerId,
                created.Name,
                created.NormalizedName,
                created.Title,
                created.Species,
                created.Gender,
                created.Type,
                created.ExperiencePoints,
                CharacterStatus.Active,
                DeathDate: null,
                created.NetworkId,
                created.QrIdentifier,
                [.. created.StatValues],
                IsArchived: false),
            CharacterDetailsChanged changed when snapshot is not null => snapshot with
            {
                Name = changed.Name,
                NormalizedName = changed.NormalizedName,
                Title = changed.Title,
                Species = changed.Species,
                Gender = changed.Gender,
                Type = changed.Type,
                ExperiencePoints = changed.ExperiencePoints
            },
            CharacterNetworkChanged network when snapshot is not null => snapshot with { NetworkId = network.NetworkId },
            CharacterStatusChanged status when snapshot is not null => snapshot with
            {
                Status = status.Status,
                DeathDate = status.DeathDate
            },
            CharacterStatValuesChanged values when snapshot is not null => snapshot with
            {
                StatValues = Merge(snapshot.StatValues, values.Values)
            },
            CharacterSkillsChanged skills when snapshot is not null => snapshot with
            {
                TalentIds = [.. skills.TalentIds],
                SkillPurchases = [.. skills.Purchases]
            },
            CharacterQrIdentifierRegenerated qr when snapshot is not null => snapshot with { QrIdentifier = qr.QrIdentifier },
            CharacterArchived when snapshot is not null => snapshot with { IsArchived = true },
            CharacterUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };

    private static CharacterStatValue[] Merge(CharacterStatValue[] current, IReadOnlyList<CharacterStatValue> changes)
    {
        var changedIds = changes.Select(change => change.StatId).ToHashSet();
        return [.. current.Where(value => !changedIds.Contains(value.StatId)), .. changes];
    }
}
