using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.CharacterStatEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

public partial class CharacterStatProjection : SingleStreamProjection<CharacterStatDocument, Guid>
{
    public CharacterStatProjection()
    {
        IncludeType<CharacterStatDefined>();
        IncludeType<CharacterStatRenamed>();
        IncludeType<CharacterStatArchived>();
        IncludeType<CharacterStatUnarchived>();
    }

    public override CharacterStatDocument? Evolve(CharacterStatDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            CharacterStatDefined defined => new CharacterStatDocument(
                defined.StatId,
                defined.Name,
                defined.NormalizedName,
                defined.Type,
                IsArchived: false),
            CharacterStatRenamed renamed when snapshot is not null => snapshot with
            {
                Name = renamed.Name,
                NormalizedName = renamed.NormalizedName
            },
            CharacterStatArchived when snapshot is not null => snapshot with { IsArchived = true },
            CharacterStatUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
