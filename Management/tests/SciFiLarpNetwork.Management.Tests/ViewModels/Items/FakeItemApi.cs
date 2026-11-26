using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Items;

/// <summary>
/// An in-memory item catalog that answers with the same Siren shape, links and actions as the backend.
/// </summary>
internal sealed class FakeItemApi : IManagementApiClient
{
    public const string ItemsHref = "/api/v1/items";
    private const string TypesHref = "/api/v1/item-types";
    private const string RaritiesHref = "/api/v1/rarities";
    private const string CardsHref = "/api/v1/item-cards";
    private const string ImagesHref = "/api/v1/item-images";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanWrite { get; init; } = true;

    public List<FakeCatalogEntry> Types { get; } = [];

    public List<FakeCatalogEntry> Rarities { get; } = [];

    public List<FakeItem> Items { get; } = [];

    public List<FakeCopy> Copies { get; } = [];

    public Dictionary<Guid, byte[]> Images { get; } = [];

    public List<Submission> Submissions { get; } = [];

    public List<string> Gets { get; } = [];

    /// <summary>One-shot failures for the next submission of the named action.</summary>
    public Dictionary<string, Exception> SubmitFailures { get; } = [];

    /// <summary>When set, the next GET of an href starting with this text waits for the task before answering.</summary>
    public (string Prefix, TaskCompletionSource Gate)? HeldGet { get; set; }

    public FakeCatalogEntry AddType(string name, bool isArchived = false)
    {
        var entry = new FakeCatalogEntry(Guid.NewGuid(), name) { IsArchived = isArchived };
        Types.Add(entry);
        return entry;
    }

    public FakeCatalogEntry AddRarity(string name, bool isArchived = false)
    {
        var entry = new FakeCatalogEntry(Guid.NewGuid(), name) { IsArchived = isArchived };
        Rarities.Add(entry);
        return entry;
    }

    public FakeItem AddItem(string name, FakeCatalogEntry? type = null, FakeCatalogEntry? rarity = null, bool isArchived = false)
    {
        var item = new FakeItem(Guid.NewGuid(), name, (type ?? Types.FirstOrDefault() ?? AddType("Tool")).Id,
            (rarity ?? Rarities.FirstOrDefault() ?? AddRarity("Common")).Id)
        { IsArchived = isArchived };
        Items.Add(item);
        return item;
    }

    public FakeCopy AddCopy(FakeItem item, bool isActive = true)
    {
        var copy = new FakeCopy(Guid.NewGuid(), item.Id, Guid.NewGuid()) { IsActive = isActive };
        Copies.Add(copy);
        return copy;
    }

    public async Task<SirenEntity> GetResourceAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default)
    {
        Gets.Add(href);
        if (HeldGet is { } held && href.StartsWith(held.Prefix, StringComparison.Ordinal))
        {
            HeldGet = null;
            await held.Gate.Task;
        }

        var parts = href.Split('?');
        var query = parts.Length > 1
            ? parts[1].Split('&').Select(pair => pair.Split('=')).ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]))
            : [];
        var segments = parts[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments switch
        {
            ["api", "v1", "items"] => ItemCollection(query),
            ["api", "v1", "items", var id] => ItemEntity(FindItem(id)),
            ["api", "v1", "items", var id, "copies"] => CopyCollection(FindItem(id)),
            ["api", "v1", "item-types"] => CatalogCollection(Types, TypesHref, "create-item-type", query.ContainsKey("includeArchived")),
            ["api", "v1", "item-types", var id] => CatalogEntity(Find(Types, id), TypesHref, "item-type", "rename-item-type", isRarity: false),
            ["api", "v1", "rarities"] => CatalogCollection(Rarities, RaritiesHref, "create-rarity", query.ContainsKey("includeArchived")),
            ["api", "v1", "rarities", var id] => CatalogEntity(Find(Rarities, id), RaritiesHref, "rarity", "rename-rarity", isRarity: true),
            _ => throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound)
        };
    }

    public Task<byte[]> GetImageAsync(string apiBaseUrl, string href, string locale, CancellationToken cancellationToken = default) =>
        Images.TryGetValue(Guid.Parse(href.Split('/')[^1]), out var image)
            ? Task.FromResult(image)
            : Task.FromException<byte[]>(new ManagementApiException("errors.notFound", HttpStatusCode.NotFound));

    public Task<byte[]> GetFileAsync(string apiBaseUrl, string href, string mediaType, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromException<byte[]>(new NotSupportedException("This fake serves no files."));

    public Task<byte[]> DownloadActionAsync(string apiBaseUrl, SirenAction action, JsonElement body, string mediaType, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body.GetRawText()));
        if (SubmitFailures.Remove(action.Name, out var failure))
        {
            return Task.FromException<byte[]>(failure);
        }

        if (action.Name == "print-item-cards")
        {
            var copies = body.GetProperty("copiesPerItem").GetInt32();
            foreach (var itemId in body.GetProperty("itemIds").EnumerateArray().Select(id => id.GetGuid()))
            {
                for (var index = 0; index < copies; index++)
                {
                    AddCopy(Items.Single(item => item.Id == itemId));
                }
            }
        }

        return Task.FromResult<byte[]>([0x25, 0x50, 0x44, 0x46]);
    }

    public Task<SirenEntity> SubmitActionAsync(string apiBaseUrl, SirenAction action, JsonElement? body, string locale,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add(new Submission(action.Name, body?.GetRawText()));
        return SubmitFailures.Remove(action.Name, out var failure)
            ? Task.FromException<SirenEntity>(failure)
            : Task.FromResult(Apply(action, body));
    }

    public Task<ManagementSession> SignInAsync(string apiBaseUrl, string userName, string password, string locale,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession(userName, "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task SignOutAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ManagementSession> RenewSessionAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ManagementSession("operator", "administrator", DateTimeOffset.UtcNow.AddHours(1)));

    public Task<ApiDiscovery> DiscoverAsync(string apiBaseUrl, string locale, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ApiDiscovery(
            new SirenEntity { Class = ["api"], Properties = Json(new { title = "Administration" }) },
            [new SirenLink { Rel = [SirenRelations.Navigation, SirenRelations.Items], Href = ItemsHref, Title = "Items" }]));

    private SirenEntity Apply(SirenAction action, JsonElement? body)
    {
        var segments = action.Href.Split('/', StringSplitOptions.RemoveEmptyEntries);
        switch (action.Name)
        {
            case "create-item":
                var created = new FakeItem(Guid.NewGuid(), string.Empty, Guid.Empty, Guid.Empty);
                ApplyDetails(created, body!.Value);
                if (body.Value.TryGetProperty("components", out var components))
                {
                    created.Components.AddRange(components.EnumerateArray()
                        .Select(component => (component.GetProperty("itemId").GetGuid(), component.GetProperty("quantity").GetInt32())));
                }

                if (body.Value.TryGetProperty("clonedFromItemId", out var source))
                {
                    created.ImageId = Items.Single(item => item.Id == source.GetGuid()).ImageId;
                }

                Items.Add(created);
                return ItemEntity(created);
            case "update-item":
                var updated = FindItem(segments[3]);
                RequireVersion(body!.Value, updated.Version);
                ApplyDetails(updated, body.Value);
                updated.Version++;
                return ItemEntity(updated);
            case "archive-item" or "unarchive-item":
                var toggled = FindItem(segments[3]);
                toggled.IsArchived = action.Name == "archive-item";
                toggled.Version++;
                return ItemEntity(toggled);
            case "set-item-image" or "remove-item-image":
                var withImage = FindItem(segments[3]);
                RequireVersion(body!.Value, withImage.Version);
                withImage.ImageId = null;
                if (action.Name == "set-item-image")
                {
                    withImage.ImageId = Guid.NewGuid();
                    Images[withImage.ImageId.Value] = body.Value.GetProperty("content").GetBytesFromBase64();
                }

                withImage.Version++;
                return ItemEntity(withImage);
            case "add-item-component":
                var parent = FindItem(segments[3]);
                RequireVersion(body!.Value, parent.Version);
                parent.Components.Add((body.Value.GetProperty("itemId").GetGuid(), body.Value.GetProperty("quantity").GetInt32()));
                parent.Version++;
                return ItemEntity(parent);
            case "change-component-quantity" or "remove-item-component":
                var owner = FindItem(segments[3]);
                RequireVersion(body!.Value, owner.Version);
                var componentId = Guid.Parse(segments[5]);
                var index = owner.Components.FindIndex(component => component.ItemId == componentId);
                if (action.Name == "remove-item-component")
                {
                    owner.Components.RemoveAt(index);
                }
                else
                {
                    owner.Components[index] = (componentId, body.Value.GetProperty("quantity").GetInt32());
                }

                owner.Version++;
                return ItemEntity(owner);
            case "deactivate-item-copy" or "reactivate-item-copy":
                var copy = Copies.Single(candidate => candidate.Id == Guid.Parse(segments[3]));
                copy.IsActive = action.Name == "reactivate-item-copy";
                return new SirenEntity { Class = ["item-copy"], Properties = CopyProperties(copy) };
            case "create-item-type" or "create-rarity":
                var entries = action.Name == "create-item-type" ? Types : Rarities;
                var entry = new FakeCatalogEntry(Guid.NewGuid(), body!.Value.GetProperty("name").GetString()!);
                entries.Add(entry);
                return CatalogEntity(entry, action.Href, "entry", "rename", action.Name == "create-rarity");
            case "rename-item-type" or "rename-rarity":
                var renamed = Find(action.Name == "rename-item-type" ? Types : Rarities, segments[3]);
                RequireVersion(body!.Value, renamed.Version);
                renamed.Name = body.Value.GetProperty("name").GetString()!;
                renamed.Version++;
                return CatalogEntity(renamed, $"/{segments[0]}/{segments[1]}/{segments[2]}", "entry", action.Name, segments[2] == "rarities");
            case "archive-item-type" or "unarchive-item-type" or "archive-rarity" or "unarchive-rarity":
                var archivable = Find(segments[2] == "rarities" ? Rarities : Types, segments[3]);
                archivable.IsArchived = action.Name.StartsWith("archive", StringComparison.Ordinal);
                archivable.Version++;
                return CatalogEntity(archivable, $"/{segments[0]}/{segments[1]}/{segments[2]}", "entry", "rename", segments[2] == "rarities");
            case "move-rarity-up" or "move-rarity-down":
                var moved = Find(Rarities, segments[3]);
                var position = Rarities.IndexOf(moved);
                var target = action.Name == "move-rarity-up" ? position - 1 : position + 1;
                if (target >= 0 && target < Rarities.Count)
                {
                    (Rarities[position], Rarities[target]) = (Rarities[target], Rarities[position]);
                }

                return CatalogEntity(moved, RaritiesHref, "rarity", "rename-rarity", isRarity: true);
            default:
                throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);
        }
    }

    private static void ApplyDetails(FakeItem item, JsonElement body)
    {
        item.Name = body.GetProperty("name").GetString()!;
        item.ItemTypeId = body.GetProperty("itemTypeId").GetGuid();
        item.RarityId = body.GetProperty("rarityId").GetGuid();
        item.Description = body.TryGetProperty("description", out var description) ? description.GetString() : null;
        item.Effect = body.TryGetProperty("effect", out var effect) ? effect.GetString() : null;
        item.Cost = body.GetProperty("cost").GetInt32();
        item.Charges = body.TryGetProperty("charges", out var charges) ? charges.GetInt32() : null;
        item.ExpiresOn = body.TryGetProperty("expiresOn", out var expires) ? DateOnly.Parse(expires.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    private static void RequireVersion(JsonElement body, long version)
    {
        if (body.GetProperty("version").GetInt64() != version)
        {
            throw new ManagementApiException("errors.staleVersion", HttpStatusCode.Conflict);
        }
    }

    private FakeItem FindItem(string id) =>
        Items.SingleOrDefault(item => item.Id == Guid.Parse(id)) ?? throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);

    private static FakeCatalogEntry Find(List<FakeCatalogEntry> entries, string id) =>
        entries.SingleOrDefault(entry => entry.Id == Guid.Parse(id)) ?? throw new ManagementApiException("errors.notFound", HttpStatusCode.NotFound);

    private SirenEntity ItemCollection(Dictionary<string, string> query)
    {
        var page = query.TryGetValue("page", out var number) ? int.Parse(number, System.Globalization.CultureInfo.InvariantCulture) : 1;
        var matching = Items
            .Where(item => query.ContainsKey("includeArchived") || !item.IsArchived)
            .Where(item => !query.TryGetValue("itemTypeId", out var typeId) || item.ItemTypeId == Guid.Parse(typeId))
            .Where(item => !query.TryGetValue("search", out var search) || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        const int pageSize = 2;
        var pageCount = Math.Max(1, (matching.Length + pageSize - 1) / pageSize);
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = PageHref(query, page) },
            new() { Rel = [SirenRelations.ItemTypes], Href = TypesHref },
            new() { Rel = [SirenRelations.Rarities], Href = RaritiesHref }
        };
        if (page < pageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(query, page + 1) });
        }

        if (page > 1)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Previous], Href = PageHref(query, page - 1) });
        }

        var actions = new List<SirenAction>
        {
            new()
            {
                Name = "search-items",
                Href = ItemsHref,
                Method = "GET",
                Fields = [new SirenField { Name = "search" }, new SirenField { Name = "itemTypeId" }, new SirenField { Name = "includeArchived" }]
            },
            Action("preview-item-cards", $"{CardsHref}/preview", "POST")
        };
        if (CanWrite)
        {
            actions.Add(Action("create-item", ItemsHref, "POST"));
            actions.Add(Action("print-item-cards", $"{CardsHref}/print", "POST"));
            actions.Add(Action("reprint-item-cards", $"{CardsHref}/reprint", "POST"));
        }

        return new SirenEntity
        {
            Class = ["item-collection"],
            Properties = Json(new
            {
                title = "Items",
                description = "Items",
                includesArchived = query.ContainsKey("includeArchived"),
                pageNumber = page,
                pageCount,
                totalCount = matching.Length,
                maximumNameLength = 64,
                maximumTextLength = 4000,
                maximumCost = 999_999,
                maximumCharges = 999,
                maximumQuantity = 99,
                maximumComponents = 20,
                maximumCopiesPerItem = 50,
                maximumCards = 200,
                maximumImageByteCount = 2 * 1024 * 1024
            }),
            Entities = [.. matching.Skip((page - 1) * pageSize).Take(pageSize).Select(item => new SirenSubEntity
            {
                Rel = ["item"],
                Href = ItemHref(item),
                Properties = ItemProperties(item)
            })],
            Links = links,
            Actions = actions
        };
    }

    private static string PageHref(Dictionary<string, string> query, int page)
    {
        var parameters = query.Where(pair => pair.Key != "page").Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}").ToList();
        if (page > 1)
        {
            parameters.Add($"page={page}");
        }

        return parameters.Count == 0 ? ItemsHref : $"{ItemsHref}?{string.Join('&', parameters)}";
    }

    private SirenEntity ItemEntity(FakeItem item)
    {
        var href = ItemHref(item);
        var actions = new List<SirenAction>
        {
            Action("update-item", href, "PUT"),
            Action(item.IsArchived ? "unarchive-item" : "archive-item", $"{href}/{(item.IsArchived ? "unarchive" : "archive")}", "POST"),
            Action("add-item-component", $"{href}/components", "POST"),
            Action("set-item-image", $"{href}/image", "PUT")
        };
        if (item.ImageId is not null)
        {
            actions.Add(Action("remove-item-image", $"{href}/image", "DELETE"));
        }

        return new SirenEntity
        {
            Class = ["item"],
            Properties = ItemProperties(item),
            Entities = [.. item.Components.Select(component => new SirenSubEntity
            {
                Rel = [SirenRelations.Component],
                Href = $"{ItemsHref}/{component.ItemId:D}",
                Properties = Json(new { itemId = component.ItemId, name = Name(component.ItemId), quantity = component.Quantity, isArchived = false }),
                Actions = CanWrite
                    ?
                    [
                        Action("change-component-quantity", $"{href}/components/{component.ItemId:D}", "PUT"),
                        Action("remove-item-component", $"{href}/components/{component.ItemId:D}", "DELETE")
                    ]
                    : null
            })],
            Links =
            [
                new SirenLink { Rel = ["self"], Href = href },
                new SirenLink { Rel = [SirenRelations.ItemCopies], Href = $"{href}/copies" },
                .. item.ImageId is { } imageId ? [new SirenLink { Rel = [SirenRelations.ItemImage], Href = $"{ImagesHref}/{imageId:D}" }] : Array.Empty<SirenLink>()
            ],
            Actions = CanWrite ? actions : null
        };
    }

    private SirenEntity CopyCollection(FakeItem item) =>
        new()
        {
            Class = ["item-copy-collection"],
            Properties = Json(new { title = "Copies", itemId = item.Id, copyCount = Copies.Count(copy => copy.ItemId == item.Id) }),
            Entities = [.. Copies.Where(copy => copy.ItemId == item.Id).Select(copy => new SirenSubEntity
            {
                Rel = ["item"],
                Href = $"/api/v1/item-copies/{copy.Id:D}",
                Properties = CopyProperties(copy),
                Actions = CanWrite
                    ?
                    [
                        copy.IsActive
                            ? Action("deactivate-item-copy", $"/api/v1/item-copies/{copy.Id:D}/deactivate", "POST")
                            : Action("reactivate-item-copy", $"/api/v1/item-copies/{copy.Id:D}/reactivate", "POST")
                    ]
                    : null
            })]
        };

    private SirenEntity CatalogCollection(List<FakeCatalogEntry> entries, string href, string createAction, bool includeArchived) =>
        new()
        {
            Class = ["catalog"],
            Properties = Json(new { title = "Catalog", description = "Catalog", includesArchived = includeArchived, maximumNameLength = 64 }),
            Entities = [.. entries.Where(entry => includeArchived || !entry.IsArchived).Select(entry => new SirenSubEntity
            {
                Rel = ["item"],
                Href = $"{href}/{entry.Id:D}",
                Properties = EntryProperties(entry, href == RaritiesHref)
            })],
            Links =
            [
                new SirenLink { Rel = ["self"], Href = href },
                new SirenLink { Rel = [SirenRelations.WithArchived], Href = $"{href}?includeArchived=true" }
            ],
            Actions = CanWrite ? [Action(createAction, href, "POST")] : null
        };

    private SirenEntity CatalogEntity(FakeCatalogEntry entry, string collectionHref, string kind, string renameAction, bool isRarity)
    {
        var href = $"{(isRarity ? RaritiesHref : TypesHref)}/{entry.Id:D}";
        var kindName = isRarity ? "rarity" : "item-type";
        var actions = new List<SirenAction>
        {
            Action(isRarity ? "rename-rarity" : "rename-item-type", href, "PUT"),
            Action(entry.IsArchived ? $"unarchive-{kindName}" : $"archive-{kindName}", $"{href}/{(entry.IsArchived ? "unarchive" : "archive")}", "POST")
        };
        if (isRarity)
        {
            actions.Add(Action("move-rarity-up", $"{href}/move-up", "POST"));
            actions.Add(Action("move-rarity-down", $"{href}/move-down", "POST"));
        }

        return new SirenEntity
        {
            Class = [kind],
            Properties = EntryProperties(entry, isRarity),
            Links = [new SirenLink { Rel = ["self"], Href = href }, new SirenLink { Rel = ["collection"], Href = collectionHref }],
            Actions = CanWrite ? actions : null
        };
    }

    private JsonElement EntryProperties(FakeCatalogEntry entry, bool isRarity) =>
        isRarity
            ? Json(new { rarityId = entry.Id, name = entry.Name, position = Rarities.IndexOf(entry), isArchived = entry.IsArchived, version = entry.Version })
            : Json(new { itemTypeId = entry.Id, name = entry.Name, isArchived = entry.IsArchived, version = entry.Version });

    private JsonElement ItemProperties(FakeItem item) =>
        Json(new
        {
            itemId = item.Id,
            name = item.Name,
            itemTypeId = item.ItemTypeId,
            rarityId = item.RarityId,
            description = item.Description,
            effect = item.Effect,
            cost = item.Cost,
            charges = item.Charges,
            expiresOn = item.ExpiresOn,
            imageId = item.ImageId,
            components = item.Components.Select(component => new
            {
                itemId = component.ItemId,
                name = Name(component.ItemId),
                quantity = component.Quantity,
                isArchived = false
            }).ToArray(),
            isArchived = item.IsArchived,
            version = item.Version
        });

    private static JsonElement CopyProperties(FakeCopy copy) =>
        Json(new
        {
            copyId = copy.Id,
            qrCode = copy.Id.ToString("N"),
            itemId = copy.ItemId,
            printId = copy.PrintId,
            printedAtUtc = new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero),
            printedBy = "operator",
            isActive = copy.IsActive,
            changedAtUtc = (DateTimeOffset?)null,
            changedBy = (string?)null
        });

    private string Name(Guid itemId) => Items.SingleOrDefault(item => item.Id == itemId)?.Name ?? string.Empty;

    private static string ItemHref(FakeItem item) => $"{ItemsHref}/{item.Id:D}";

    private static SirenAction Action(string name, string href, string method) =>
        new() { Name = name, Href = href, Method = method, Type = "application/json" };

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, JsonOptions);
}
