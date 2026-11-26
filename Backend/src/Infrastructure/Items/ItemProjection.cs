using JasperFx.Events;
using Marten.Events.Aggregation;
using SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

public partial class ItemProjection : SingleStreamProjection<ItemDocument, Guid>
{
    public ItemProjection()
    {
        IncludeType<ItemCreated>();
        IncludeType<ItemDetailsChanged>();
        IncludeType<ItemComponentsChanged>();
        IncludeType<ItemImageSet>();
        IncludeType<ItemImageRemoved>();
        IncludeType<ItemArchived>();
        IncludeType<ItemUnarchived>();
    }

    public override ItemDocument? Evolve(ItemDocument? snapshot, Guid id, IEvent e) =>
        e.Data switch
        {
            ItemCreated created => new ItemDocument(
                created.ItemId,
                created.Name,
                created.NormalizedName,
                created.ItemTypeId,
                created.RarityId,
                created.Description,
                created.Effect,
                created.Cost,
                created.Charges,
                created.ExpiresOn,
                [.. created.Components],
                created.RecipeKey,
                ItemSearch.Text(created.Name, created.Description, created.Effect),
                ImageId: null,
                IsArchived: false),
            ItemDetailsChanged changed when snapshot is not null => snapshot with
            {
                Name = changed.Name,
                NormalizedName = changed.NormalizedName,
                ItemTypeId = changed.ItemTypeId,
                RarityId = changed.RarityId,
                Description = changed.Description,
                Effect = changed.Effect,
                Cost = changed.Cost,
                Charges = changed.Charges,
                ExpiresOn = changed.ExpiresOn,
                SearchText = ItemSearch.Text(changed.Name, changed.Description, changed.Effect)
            },
            ItemComponentsChanged changed when snapshot is not null => snapshot with
            {
                Components = [.. changed.Components],
                RecipeKey = changed.RecipeKey
            },
            ItemImageSet set when snapshot is not null => snapshot with { ImageId = set.ImageId },
            ItemImageRemoved when snapshot is not null => snapshot with { ImageId = null },
            ItemArchived when snapshot is not null => snapshot with { IsArchived = true },
            ItemUnarchived when snapshot is not null => snapshot with { IsArchived = false },
            _ => snapshot
        };
}
