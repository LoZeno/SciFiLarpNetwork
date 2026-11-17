using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.PlayerEvents;
using SciFiLarpNetwork.Backend.Domain.Players;
using SciFiLarpNetwork.Backend.Infrastructure.Players.PlayerDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Players;

public partial class PlayerProjection : SingleStreamProjection<PlayerDocument, Guid>
{
    public PlayerProjection()
    {
        IncludeType<PlayerRegistered>();
        IncludeType<PlayerDetailsChanged>();
        IncludeType<PlayerDeactivated>();
        IncludeType<PlayerActivated>();
        IncludeType<PlayerArchived>();
        IncludeType<PlayerUnarchived>();
        IncludeType<PlayerCharacterActivated>();
    }

    public override PlayerDocument? Evolve(PlayerDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            PlayerRegistered registered => new PlayerDocument(
                registered.PlayerId,
                registered.Number,
                registered.DisplayName,
                registered.NormalizedDisplayName,
                registered.Email,
                registered.Phone,
                registered.Notes,
                registered.Type,
                PlayerStatus.Active,
                IsArchived: false),
            PlayerDetailsChanged changed when snapshot is not null => snapshot with
            {
                DisplayName = changed.DisplayName,
                NormalizedDisplayName = changed.NormalizedDisplayName,
                Email = changed.Email,
                Phone = changed.Phone,
                Notes = changed.Notes,
                Type = changed.Type
            },
            PlayerDeactivated when snapshot is not null => snapshot with { Status = PlayerStatus.Inactive },
            PlayerActivated when snapshot is not null => snapshot with { Status = PlayerStatus.Active },
            PlayerArchived when snapshot is not null => snapshot with { IsArchived = true },
            PlayerUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
