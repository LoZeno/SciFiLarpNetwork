using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks;

public partial class NetworkProjection : SingleStreamProjection<NetworkDocument, Guid>
{
    public NetworkProjection()
    {
        IncludeType<NetworkCreated>();
        IncludeType<NetworkDetailsChanged>();
        IncludeType<NetworkBackgroundImageSet>();
        IncludeType<NetworkBackgroundImageRemoved>();
        IncludeType<NetworkEnabled>();
        IncludeType<NetworkDisabled>();
        IncludeType<NetworkMadeDefault>();
        IncludeType<NetworkDefaultCleared>();
    }

    public override NetworkDocument? Evolve(NetworkDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            NetworkCreated created => new NetworkDocument(
                created.NetworkId,
                created.Code,
                created.Name,
                created.NormalizedName,
                created.Description,
                created.Theme,
                BackgroundImageId: null,
                BackgroundImageMediaType: null,
                BackgroundImageByteCount: 0,
                IsEnabled: true,
                IsDefault: false),
            NetworkDetailsChanged changed when snapshot is not null => snapshot with
            {
                Name = changed.Name,
                NormalizedName = changed.NormalizedName,
                Description = changed.Description,
                Theme = changed.Theme
            },
            NetworkBackgroundImageSet image when snapshot is not null => snapshot with
            {
                BackgroundImageId = image.ImageId,
                BackgroundImageMediaType = image.MediaType,
                BackgroundImageByteCount = image.ByteCount
            },
            NetworkBackgroundImageRemoved when snapshot is not null => snapshot with
            {
                BackgroundImageId = null,
                BackgroundImageMediaType = null,
                BackgroundImageByteCount = 0
            },
            NetworkEnabled when snapshot is not null => snapshot with { IsEnabled = true },
            NetworkDisabled when snapshot is not null => snapshot with { IsEnabled = false },
            NetworkMadeDefault when snapshot is not null => snapshot with { IsDefault = true },
            NetworkDefaultCleared when snapshot is not null => snapshot with { IsDefault = false },
            _ => snapshot
        };
}
