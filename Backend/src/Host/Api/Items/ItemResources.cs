using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Items;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>
/// Builds the Siren representations of item types, rarities, items and copies. Write actions are advertised only when
/// <paramref name="canWrite"/> is set; the server still authorizes every request.
/// </summary>
internal sealed class ItemResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string ItemTypesPath => $"{pathBase}/api/v1/item-types";

    public string RaritiesPath => $"{pathBase}/api/v1/rarities";

    public string ItemsPath => $"{pathBase}/api/v1/items";

    public string ItemCardsPath => $"{pathBase}/api/v1/item-cards";

    public string ItemTypePath(Guid itemTypeId) => $"{ItemTypesPath}/{itemTypeId:D}";

    public string RarityPath(Guid rarityId) => $"{RaritiesPath}/{rarityId:D}";

    public string ItemPath(Guid itemId) => $"{ItemsPath}/{itemId:D}";

    public string ItemCopiesPath(Guid itemId) => $"{ItemPath(itemId)}/copies";

    public string CopyPath(Guid copyId) => $"{pathBase}/api/v1/item-copies/{copyId:D}";

    public string ImagePath(Guid imageId) => $"{pathBase}/api/v1/item-images/{imageId:D}";

    public SirenEntity<ItemCatalogCollectionProperties> ItemTypeCollection(IReadOnlyList<ItemTypeSummary> types, bool includeArchived) =>
        new()
        {
            Class = ["item-type-collection"],
            Properties = CatalogProperties("api.itemTypes", "api.itemTypesDescription", includeArchived),
            Entities = [.. types.Select(type => ListItem("item-type", ItemTypePath(type.ItemTypeId), type))],
            Links = [.. CatalogLinks(ItemTypesPath, includeArchived)],
            Actions = canWrite ? [CreateAction("create-item-type", ItemTypesPath, "api.createItemType")] : null
        };

    public SirenEntity<ItemTypeSummary> ItemType(ItemTypeSummary type) =>
        new()
        {
            Class = ["item-type"],
            Properties = type,
            Links = [Self(ItemTypePath(type.ItemTypeId)), Collection(ItemTypesPath)],
            Actions = canWrite
                ?
                [
                    RenameAction("rename-item-type", ItemTypePath(type.ItemTypeId), "api.renameItemType", type.Version, type.Name),
                    ArchiveAction("item-type", "ItemType", ItemTypePath(type.ItemTypeId), type.IsArchived)
                ]
                : null
        };

    public SirenEntity<ItemCatalogCollectionProperties> RarityCollection(IReadOnlyList<RaritySummary> rarities, bool includeArchived) =>
        new()
        {
            Class = ["rarity-collection"],
            Properties = CatalogProperties("api.rarities", "api.raritiesDescription", includeArchived),
            Entities = [.. rarities.Select(rarity => ListItem("rarity", RarityPath(rarity.RarityId), rarity))],
            Links = [.. CatalogLinks(RaritiesPath, includeArchived)],
            Actions = canWrite ? [CreateAction("create-rarity", RaritiesPath, "api.createRarity")] : null
        };

    public SirenEntity<RaritySummary> Rarity(RaritySummary rarity) =>
        new()
        {
            Class = ["rarity"],
            Properties = rarity,
            Links = [Self(RarityPath(rarity.RarityId)), Collection(RaritiesPath)],
            Actions = canWrite
                ?
                [
                    RenameAction("rename-rarity", RarityPath(rarity.RarityId), "api.renameRarity", rarity.Version, rarity.Name),
                    PostAction("move-rarity-up", $"{RarityPath(rarity.RarityId)}/move-up", "api.moveRarityUp"),
                    PostAction("move-rarity-down", $"{RarityPath(rarity.RarityId)}/move-down", "api.moveRarityDown"),
                    ArchiveAction("rarity", "Rarity", RarityPath(rarity.RarityId), rarity.IsArchived)
                ]
                : null
        };

    public SirenEntity<ItemCollectionProperties> ItemCollection(ItemPage page, ItemQuery query) =>
        new()
        {
            Class = ["item-collection"],
            Properties = new ItemCollectionProperties(
                text.GetLocalizedText("api.items"),
                text.GetLocalizedText("api.itemsDescription"),
                string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
                query.ItemTypeId,
                query.IncludeArchived,
                page.PageNumber,
                page.PageCount,
                page.TotalCount,
                CatalogNames.MaximumLength,
                ItemTexts.MaximumTextLength,
                ItemTexts.MaximumCost,
                ItemTexts.MaximumCharges,
                ItemRecipes.MaximumQuantity,
                ItemRecipes.MaximumComponents,
                IItemPrintService.MaxCopiesPerItem,
                IItemPrintService.MaxCards,
                NetworkImageFormats.MaximumByteCount),
            Entities = [.. page.Items.Select(item => ListItem("item", ItemPath(item.ItemId), item))],
            Links =
            [
                .. PageLinks(page, query),
                new SirenLink { Rel = [SirenRelations.ItemTypes], Href = ItemTypesPath, Title = text.GetLocalizedText("api.itemTypes") },
                new SirenLink { Rel = [SirenRelations.Rarities], Href = RaritiesPath, Title = text.GetLocalizedText("api.rarities") }
            ],
            Actions =
            [
                new SirenAction
                {
                    Name = "search-items",
                    Href = ItemsPath,
                    Method = "GET",
                    Title = text.GetLocalizedText("api.searchItems"),
                    Fields =
                    [
                        Field("search", "search", "items.search", string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim()),
                        Field("itemTypeId", "text", "items.type", query.ItemTypeId),
                        Field("includeArchived", "checkbox", "items.includeArchived", query.IncludeArchived)
                    ]
                },
                CardAction("preview-item-cards", $"{ItemCardsPath}/preview", "api.previewItemCards",
                    [Field("itemIds", "hidden", "items.items", Array.Empty<Guid>()), Field("copiesPerItem", "number", "items.copiesPerItem", 1)]),
                .. canWrite
                    ? (SirenAction[])
                    [
                        new SirenAction
                        {
                            Name = "create-item",
                            Href = ItemsPath,
                            Method = "POST",
                            Type = JsonType,
                            Title = text.GetLocalizedText("api.createItem"),
                            Fields =
                            [
                                .. ItemFields(null),
                                Field("components", "list", "items.components", Array.Empty<object>()),
                                Field("clonedFromItemId", "hidden", "items.clonedFrom", (Guid?)null)
                            ]
                        },
                        CardAction("print-item-cards", $"{ItemCardsPath}/print", "api.printItemCards",
                        [
                            Field("printId", "hidden", "items.printId", (Guid?)null),
                            Field("itemIds", "hidden", "items.items", Array.Empty<Guid>()),
                            Field("copiesPerItem", "number", "items.copiesPerItem", 1)
                        ]),
                        CardAction("reprint-item-cards", $"{ItemCardsPath}/reprint", "api.reprintItemCards",
                            [Field("copyIds", "hidden", "items.copies", Array.Empty<Guid>())])
                    ]
                    : []
            ]
        };

    public SirenEntity<ItemSummary> Item(ItemSummary item)
    {
        var path = ItemPath(item.ItemId);
        return new SirenEntity<ItemSummary>
        {
            Class = ["item"],
            Properties = item,
            Entities = [.. item.Components.Select(component => ComponentEntity(item, component))],
            Links =
            [
                Self(path),
                Collection(ItemsPath),
                new SirenLink { Rel = [SirenRelations.ItemCopies], Href = ItemCopiesPath(item.ItemId), Title = text.GetLocalizedText("api.itemCopies") },
                .. item.ImageId is { } imageId ? [new SirenLink { Rel = [SirenRelations.ItemImage], Href = ImagePath(imageId) }] : Array.Empty<SirenLink>()
            ],
            Actions = canWrite ? ItemActions(item) : null
        };
    }

    public SirenEntity<ItemCopyCollectionProperties> CopyCollection(Guid itemId, IReadOnlyList<ItemCopySummary> copies) =>
        new()
        {
            Class = ["item-copy-collection"],
            Properties = new ItemCopyCollectionProperties(text.GetLocalizedText("api.itemCopies"), itemId, copies.Count),
            Entities = [.. copies.Select(CopyEntity)],
            Links = [Self(ItemCopiesPath(itemId)), new SirenLink { Rel = ["up"], Href = ItemPath(itemId) }]
        };

    public SirenEntity<ItemCopySummary> Copy(ItemCopySummary copy) =>
        new()
        {
            Class = ["item-copy"],
            Properties = copy,
            Links = [Self(CopyPath(copy.CopyId)), new SirenLink { Rel = ["up"], Href = ItemCopiesPath(copy.ItemId) }],
            Actions = canWrite ? [CopyAction(copy)] : null
        };

    private SirenSubEntity CopyEntity(ItemCopySummary copy) =>
        new()
        {
            Rel = ["item"],
            Class = ["item-copy"],
            Href = CopyPath(copy.CopyId),
            Properties = SirenJson.ToElement(copy),
            Actions = canWrite ? [CopyAction(copy)] : null
        };

    private SirenAction CopyAction(ItemCopySummary copy) =>
        copy.IsActive
            ? PostAction("deactivate-item-copy", $"{CopyPath(copy.CopyId)}/deactivate", "api.deactivateItemCopy")
            : PostAction("reactivate-item-copy", $"{CopyPath(copy.CopyId)}/reactivate", "api.reactivateItemCopy");

    private SirenAction[] ItemActions(ItemSummary item)
    {
        var path = ItemPath(item.ItemId);
        List<SirenAction> actions =
        [
            new()
            {
                Name = "update-item",
                Href = path,
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.updateItem"),
                Fields = [VersionField(item.Version), .. ItemFields(item)]
            },
            ArchiveAction("item", "Item", path, item.IsArchived),
            new()
            {
                Name = "add-item-component",
                Href = $"{path}/components",
                Method = "POST",
                Type = JsonType,
                Title = text.GetLocalizedText("api.addItemComponent"),
                Fields = [VersionField(item.Version), Field("itemId", "text", "items.component", (Guid?)null), Field("quantity", "number", "items.quantity", 1)]
            },
            new()
            {
                Name = "set-item-image",
                Href = $"{path}/image",
                Method = "PUT",
                Type = JsonType,
                Title = text.GetLocalizedText("api.setItemImage"),
                Fields = [VersionField(item.Version), Field("content", "file", "items.image", (string?)null)]
            }
        ];
        if (item.ImageId is not null)
        {
            actions.Add(new SirenAction
            {
                Name = "remove-item-image",
                Href = $"{path}/image",
                Method = "DELETE",
                Type = JsonType,
                Title = text.GetLocalizedText("api.removeItemImage"),
                Fields = [VersionField(item.Version)]
            });
        }

        return [.. actions];
    }

    private SirenSubEntity ComponentEntity(ItemSummary item, ItemComponentSummary component)
    {
        var path = $"{ItemPath(item.ItemId)}/components/{component.ItemId:D}";
        return new SirenSubEntity
        {
            Rel = [SirenRelations.Component],
            Class = ["item-component"],
            Href = ItemPath(component.ItemId),
            Properties = SirenJson.ToElement(component),
            Actions = canWrite
                ?
                [
                    new SirenAction
                    {
                        Name = "change-component-quantity",
                        Href = path,
                        Method = "PUT",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.changeComponentQuantity"),
                        Fields = [VersionField(item.Version), Field("quantity", "number", "items.quantity", component.Quantity)]
                    },
                    new SirenAction
                    {
                        Name = "remove-item-component",
                        Href = path,
                        Method = "DELETE",
                        Type = JsonType,
                        Title = text.GetLocalizedText("api.removeItemComponent"),
                        Fields = [VersionField(item.Version)]
                    }
                ]
                : null
        };
    }

    private ItemCatalogCollectionProperties CatalogProperties(string titleKey, string descriptionKey, bool includeArchived) =>
        new(text.GetLocalizedText(titleKey), text.GetLocalizedText(descriptionKey), includeArchived, CatalogNames.MaximumLength);

    private SirenLink[] CatalogLinks(string path, bool includeArchived) =>
    [
        Self(includeArchived ? $"{path}?includeArchived=true" : path),
        new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{path}?includeArchived=true" },
        new SirenLink { Rel = [SirenRelations.WithoutArchived], Href = path },
        new SirenLink { Rel = [SirenRelations.Items], Href = ItemsPath, Title = text.GetLocalizedText("api.items") },
        new SirenLink { Rel = [SirenRelations.ItemTypes], Href = ItemTypesPath, Title = text.GetLocalizedText("api.itemTypes") },
        new SirenLink { Rel = [SirenRelations.Rarities], Href = RaritiesPath, Title = text.GetLocalizedText("api.rarities") }
    ];

    private SirenLink[] PageLinks(ItemPage page, ItemQuery query)
    {
        var links = new List<SirenLink>
        {
            Self(PageHref(query)),
            new() { Rel = [SirenRelations.First], Href = PageHref(query with { PageNumber = 1 }) },
            new() { Rel = [SirenRelations.Last], Href = PageHref(query with { PageNumber = page.PageCount }) },
            new() { Rel = [SirenRelations.WithArchived], Href = PageHref(query with { IncludeArchived = true, PageNumber = 1 }) },
            new() { Rel = [SirenRelations.WithoutArchived], Href = PageHref(query with { IncludeArchived = false, PageNumber = 1 }) }
        };
        if (page.PageNumber > 1)
        {
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Previous],
                Href = PageHref(query with { PageNumber = Math.Min(page.PageNumber - 1, page.PageCount) })
            });
        }

        if (page.PageNumber < page.PageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(query with { PageNumber = page.PageNumber + 1 }) });
        }

        return [.. links];
    }

    private string PageHref(ItemQuery query)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (query.ItemTypeId is { } itemTypeId)
        {
            parameters.Add($"itemTypeId={itemTypeId:D}");
        }

        if (query.IncludeArchived)
        {
            parameters.Add("includeArchived=true");
        }

        if (query.PageNumber > 1)
        {
            parameters.Add($"page={query.PageNumber}");
        }

        return parameters.Count == 0 ? ItemsPath : $"{ItemsPath}?{string.Join('&', parameters)}";
    }

    private static SirenSubEntity ListItem<T>(string itemClass, string href, T properties) =>
        new() { Rel = ["item"], Class = [itemClass], Href = href, Properties = SirenJson.ToElement(properties) };

    private static SirenLink Self(string href) => new() { Rel = ["self"], Href = href };

    private static SirenLink Collection(string href) => new() { Rel = ["collection"], Href = href };

    private SirenAction CreateAction(string name, string href, string titleKey) =>
        new()
        {
            Name = name,
            Href = href,
            Method = "POST",
            Type = JsonType,
            Title = text.GetLocalizedText(titleKey),
            Fields = [Field("name", "text", "items.name", (string?)null)]
        };

    private SirenAction RenameAction(string name, string href, string titleKey, long version, string currentName) =>
        new()
        {
            Name = name,
            Href = href,
            Method = "PUT",
            Type = JsonType,
            Title = text.GetLocalizedText(titleKey),
            Fields = [VersionField(version), Field("name", "text", "items.name", currentName)]
        };

    private SirenAction PostAction(string name, string href, string titleKey) =>
        new() { Name = name, Href = href, Method = "POST", Type = JsonType, Title = text.GetLocalizedText(titleKey) };

    private SirenAction CardAction(string name, string href, string titleKey, SirenField[] fields) =>
        new() { Name = name, Href = href, Method = "POST", Type = JsonType, Title = text.GetLocalizedText(titleKey), Fields = fields };

    private SirenAction ArchiveAction(string kind, string titleKind, string resourcePath, bool isArchived) =>
        isArchived
            ? PostAction($"unarchive-{kind}", $"{resourcePath}/unarchive", $"api.unarchive{titleKind}")
            : PostAction($"archive-{kind}", $"{resourcePath}/archive", $"api.archive{titleKind}");

    private SirenField VersionField(long version) =>
        new() { Name = "version", Type = "hidden", Value = SirenJson.ToElement(version), Title = text.GetLocalizedText("items.version") };

    private SirenField[] ItemFields(ItemSummary? item) =>
    [
        Field("name", "text", "items.name", item?.Name),
        Field("itemTypeId", "text", "items.type", item?.ItemTypeId),
        Field("rarityId", "text", "items.rarity", item?.RarityId),
        Field("description", "textarea", "items.description", item?.Description),
        Field("effect", "textarea", "items.effect", item?.Effect),
        Field("cost", "number", "items.cost", item?.Cost),
        Field("charges", "number", "items.charges", item?.Charges),
        Field("expiresOn", "date", "items.expiresOn", item?.ExpiresOn)
    ];

    private SirenField Field<T>(string name, string type, string titleKey, T? value) =>
        new()
        {
            Name = name,
            Type = type,
            Title = text.GetLocalizedText(titleKey),
            Value = value is null ? null : SirenJson.ToElement(value)
        };
}
