using JasperFx.Events;
using Marten;
using Marten.Pagination;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Domain.Events.ItemEvents;
using SciFiLarpNetwork.Backend.Domain.Items;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items;

/// <summary>
/// Item commands follow Marten's command handler workflow: one session, <c>FetchForWriting</c> on every stream the
/// decision depends on, and one <c>SaveChangesAsync</c>. Streams that are read but not appended to (the type, the rarity,
/// the components) are marked <c>AlwaysEnforceConsistency</c>, so a change committed to them first rejects the commit.
/// Component additions are also serialized through the single item-composition stream, because two additions that
/// commit at the same moment could otherwise together make an item contain itself. Unique indexes on the normalized
/// name and on the recipe key reject a concurrent command that claimed the same name or recipe.
/// </summary>
internal sealed class ItemService(AttributedSessionFactory sessions, TimeProvider timeProvider) : IItemService
{
    public async Task<ItemPage> GetItemsAsync(ItemQuery query, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        IQueryable<ItemDocument> items = session.Query<ItemDocument>();
        if (!query.IncludeArchived)
        {
            items = items.Where(item => !item.IsArchived);
        }

        if (query.ItemTypeId is { } itemTypeId)
        {
            items = items.Where(item => item.ItemTypeId == itemTypeId);
        }

        if (ItemSearch.Normalize(query.Search) is { } search)
        {
            items = items.Where(item => item.SearchText.Contains(search));
        }

        var page = await items
            .OrderBy(item => item.NormalizedName)
            .ThenBy(item => item.Id)
            .ToPagedListAsync(query.PageNumber, ItemQuery.PageSize, cancellationToken);
        return new ItemPage(
            await session.SummarizeAsync([.. page], cancellationToken),
            (int)page.PageNumber,
            Math.Max(1, (int)page.PageCount),
            page.TotalItemCount);
    }

    public async Task<ItemSummary?> GetItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.QuerySession();
        return await session.LoadAsync<ItemDocument>(itemId, cancellationToken) is { } item
            ? await session.SummarizeAsync(item, cancellationToken)
            : null;
    }

    public async Task<ItemResult<ItemSummary>> CreateItemAsync(ItemCreation creation, CancellationToken cancellationToken = default)
    {
        var definition = creation.Definition;
        var itemId = Guid.NewGuid();
        if (ItemDefinitionValidator.Validate(definition) is var invalid and not ItemOutcome.Succeeded)
        {
            return Failure(invalid);
        }

        if (ToOutcome(ItemRecipes.FindStructuralProblem(itemId, creation.Components)) is var recipeProblem
            and not ItemOutcome.Succeeded)
        {
            return Failure(recipeProblem);
        }

        await using var session = sessions.OpenSession();
        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, itemId, cancellationToken))
        {
            return Failure(ItemOutcome.NameAlreadyUsed);
        }

        if (await LockCatalogEntriesAsync(session, definition, current: null, cancellationToken) is var catalogOutcome
            and not ItemOutcome.Succeeded)
        {
            return Failure(catalogOutcome);
        }

        ItemDocument? source = null;
        if (creation.ClonedFromItemId is { } sourceId &&
            (source = await session.LoadAsync<ItemDocument>(sourceId, cancellationToken)) is null)
        {
            return Failure(ItemOutcome.UnknownCloneSource);
        }

        ItemComponent[] components = [.. creation.Components];
        var (componentOutcome, _) = await LockAddableComponentsAsync(
            session, [.. components.Select(component => component.ItemId)], cancellationToken);
        if (componentOutcome != ItemOutcome.Succeeded)
        {
            return Failure(componentOutcome);
        }

        var recipeKey = ItemRecipes.Key(itemId, components);
        if (await IsRecipeUsedAsync(session, recipeKey, itemId, cancellationToken))
        {
            return Failure(ItemOutcome.DuplicateRecipe);
        }

        // A new item is not a component of any other item, so its components cannot close a loop.
        var now = timeProvider.GetUtcNow();
        List<object> events = [new ItemCreated(
            itemId,
            definition.Name.Trim(),
            normalizedName,
            definition.ItemTypeId,
            definition.RarityId,
            ItemTexts.Normalize(definition.Description),
            ItemTexts.Normalize(definition.Effect),
            definition.Cost,
            definition.Charges,
            definition.ExpiresOn,
            components,
            recipeKey,
            source?.Id,
            now)];
        if (source?.ImageId is { } imageId &&
            await session.LoadAsync<ItemImageFileDocument>(imageId, cancellationToken) is { } image)
        {
            events.Add(new ItemImageSet(itemId, image.Id, image.MediaType, image.ByteCount, now));
        }

        session.Events.StartStream<ItemDocument>(itemId, [.. events]);
        return await session.SaveItemAsync(sessions, itemId, editedVersion: null,
            ItemWrites.NameOrRecipeConflict(sessions, itemId, normalizedName), cancellationToken);
    }

    public async Task<ItemResult<ItemSummary>> UpdateItemAsync(Guid itemId, long expectedVersion, ItemDefinition definition,
        CancellationToken cancellationToken = default)
    {
        if (ItemDefinitionValidator.Validate(definition) is var invalid and not ItemOutcome.Succeeded)
        {
            return Failure(invalid);
        }

        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemDocument>(itemId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        var normalizedName = CatalogNames.Normalize(definition.Name);
        if (await IsNameUsedAsync(session, normalizedName, itemId, cancellationToken))
        {
            return Failure(ItemOutcome.NameAlreadyUsed);
        }

        if (await LockCatalogEntriesAsync(session, definition, current, cancellationToken) is var catalogOutcome
            and not ItemOutcome.Succeeded)
        {
            return Failure(catalogOutcome);
        }

        var changed = new ItemDetailsChanged(
            itemId,
            definition.Name.Trim(),
            normalizedName,
            definition.ItemTypeId,
            definition.RarityId,
            ItemTexts.Normalize(definition.Description),
            ItemTexts.Normalize(definition.Effect),
            definition.Cost,
            definition.Charges,
            definition.ExpiresOn,
            timeProvider.GetUtcNow());
        if (SameDetails(current, changed))
        {
            return ItemResult<ItemSummary>.Success(await session.SummarizeAsync(current, cancellationToken));
        }

        stream.AppendOne(changed);
        return await session.SaveItemAsync(sessions, itemId, expectedVersion,
            ItemWrites.NameOrRecipeConflict(sessions, itemId, normalizedName), cancellationToken);
    }

    public Task<ItemResult<ItemSummary>> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(itemId, archived: true, cancellationToken);

    public Task<ItemResult<ItemSummary>> UnarchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        SetArchivedAsync(itemId, archived: false, cancellationToken);

    public async Task<ItemResult<ItemSummary>> AddComponentAsync(Guid itemId, long expectedVersion, Guid componentItemId,
        int quantity, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemDocument>(itemId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        if (current.Components.Any(component => component.ItemId == componentItemId))
        {
            return Failure(ItemOutcome.ComponentAlreadyAdded);
        }

        ItemComponent[] components = [.. current.Components, new ItemComponent(componentItemId, quantity)];
        if (ToOutcome(ItemRecipes.FindStructuralProblem(itemId, components)) is var recipeProblem
            and not ItemOutcome.Succeeded)
        {
            return Failure(recipeProblem);
        }

        var (componentOutcome, added) = await LockAddableComponentsAsync(session, [componentItemId], cancellationToken);
        if (componentOutcome != ItemOutcome.Succeeded)
        {
            return Failure(componentOutcome);
        }

        if (await LockCompositionAsync(session, itemId, added[0], cancellationToken))
        {
            return Failure(ItemOutcome.ComponentLoop);
        }

        return await ChangeComponentsAsync(session, stream, current, components, expectedVersion, cancellationToken);
    }

    public async Task<ItemResult<ItemSummary>> ChangeComponentQuantityAsync(Guid itemId, long expectedVersion,
        Guid componentItemId, int quantity, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemDocument>(itemId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        if (current.Components.All(component => component.ItemId != componentItemId))
        {
            return Failure(ItemOutcome.NotAComponent);
        }

        ItemComponent[] components =
        [
            .. current.Components.Select(component =>
                component.ItemId == componentItemId ? component with { Quantity = quantity } : component)
        ];
        if (ToOutcome(ItemRecipes.FindStructuralProblem(itemId, components)) is var recipeProblem
            and not ItemOutcome.Succeeded)
        {
            return Failure(recipeProblem);
        }

        return await ChangeComponentsAsync(session, stream, current, components, expectedVersion, cancellationToken);
    }

    public async Task<ItemResult<ItemSummary>> RemoveComponentAsync(Guid itemId, long expectedVersion, Guid componentItemId,
        CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        var (outcome, stream) = await session.FetchExpectedAsync<ItemDocument>(itemId, expectedVersion, cancellationToken);
        if (stream?.Aggregate is not { } current)
        {
            return Failure(outcome);
        }

        if (current.Components.All(component => component.ItemId != componentItemId))
        {
            return Failure(ItemOutcome.NotAComponent);
        }

        ItemComponent[] components = [.. current.Components.Where(component => component.ItemId != componentItemId)];
        return await ChangeComponentsAsync(session, stream, current, components, expectedVersion, cancellationToken);
    }

    private async Task<ItemResult<ItemSummary>> ChangeComponentsAsync(
        IDocumentSession session,
        IEventStream<ItemDocument> stream,
        ItemDocument current,
        ItemComponent[] components,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var recipeKey = ItemRecipes.Key(current.Id, components);
        if (recipeKey == current.RecipeKey)
        {
            return ItemResult<ItemSummary>.Success(await session.SummarizeAsync(current, cancellationToken));
        }

        if (await IsRecipeUsedAsync(session, recipeKey, current.Id, cancellationToken))
        {
            return Failure(ItemOutcome.DuplicateRecipe);
        }

        stream.AppendOne(new ItemComponentsChanged(current.Id, components, recipeKey, timeProvider.GetUtcNow()));
        return await session.SaveItemAsync(sessions, current.Id, expectedVersion,
            ItemWrites.NameOrRecipeConflict(sessions, current.Id, current.NormalizedName), cancellationToken);
    }

    private async Task<ItemResult<ItemSummary>> SetArchivedAsync(Guid itemId, bool archived, CancellationToken cancellationToken)
    {
        await using var session = sessions.OpenSession();
        var stream = await session.Events.FetchForWriting<ItemDocument>(itemId, cancellationToken);
        if (stream.Aggregate is not { } current)
        {
            return Failure(ItemOutcome.NotFound);
        }

        if (current.IsArchived == archived)
        {
            return ItemResult<ItemSummary>.Success(await session.SummarizeAsync(current, cancellationToken));
        }

        var now = timeProvider.GetUtcNow();
        stream.AppendOne(archived ? new ItemArchived(itemId, now) : new ItemUnarchived(itemId, now));
        return await session.SaveItemAsync(sessions, itemId, current.Version,
            ItemWrites.NameOrRecipeConflict(sessions, itemId, current.NormalizedName), cancellationToken);
    }

    private static ItemResult<ItemSummary> Failure(ItemOutcome outcome) => ItemResult<ItemSummary>.Failure(outcome);

    private static ItemOutcome ToOutcome(ItemRecipeProblem problem) =>
        problem switch
        {
            ItemRecipeProblem.InvalidQuantity => ItemOutcome.InvalidQuantity,
            ItemRecipeProblem.TooManyComponents => ItemOutcome.TooManyComponents,
            ItemRecipeProblem.SelfComponent => ItemOutcome.SelfComponent,
            ItemRecipeProblem.DuplicateComponent => ItemOutcome.ComponentAlreadyAdded,
            _ => ItemOutcome.Succeeded
        };

    private static bool SameDetails(ItemDocument current, ItemDetailsChanged changed) =>
        current.Name == changed.Name &&
        current.ItemTypeId == changed.ItemTypeId &&
        current.RarityId == changed.RarityId &&
        current.Description == changed.Description &&
        current.Effect == changed.Effect &&
        current.Cost == changed.Cost &&
        current.Charges == changed.Charges &&
        current.ExpiresOn == changed.ExpiresOn;

    private static Task<bool> IsNameUsedAsync(IQuerySession session, string normalizedName, Guid itemId,
        CancellationToken cancellationToken) =>
        session.Query<ItemDocument>().AnyAsync(item => item.NormalizedName == normalizedName && item.Id != itemId, cancellationToken);

    private static Task<bool> IsRecipeUsedAsync(IQuerySession session, string recipeKey, Guid itemId,
        CancellationToken cancellationToken) =>
        session.Query<ItemDocument>().AnyAsync(item => item.RecipeKey == recipeKey && item.Id != itemId, cancellationToken);

    /// <summary>
    /// Checks a new or changed type and rarity: each must exist and not be archived. An edit may keep a type or rarity that
    /// was archived since.
    /// </summary>
    private static async Task<ItemOutcome> LockCatalogEntriesAsync(
        IDocumentSession session,
        ItemDefinition definition,
        ItemDocument? current,
        CancellationToken cancellationToken)
    {
        if (current?.ItemTypeId != definition.ItemTypeId)
        {
            var type = await session.Events.FetchForWriting<ItemTypeDocument>(definition.ItemTypeId, cancellationToken);
            switch (type.Aggregate)
            {
                case null:
                    return ItemOutcome.UnknownItemType;
                case { IsArchived: true }:
                    return ItemOutcome.ItemTypeArchived;
                default:
                    type.AlwaysEnforceConsistency = true;
                    break;
            }
        }

        if (current?.RarityId != definition.RarityId)
        {
            var rarity = await session.Events.FetchForWriting<RarityDocument>(definition.RarityId, cancellationToken);
            switch (rarity.Aggregate)
            {
                case null:
                    return ItemOutcome.UnknownRarity;
                case { IsArchived: true }:
                    return ItemOutcome.RarityArchived;
                default:
                    rarity.AlwaysEnforceConsistency = true;
                    break;
            }
        }

        return ItemOutcome.Succeeded;
    }

    /// <summary>Checks that every component exists and is not archived, and locks each one's stream.</summary>
    /// <returns>The component items, in the order of <paramref name="componentIds"/>.</returns>
    private static async Task<(ItemOutcome Outcome, IReadOnlyList<ItemDocument> Components)> LockAddableComponentsAsync(
        IDocumentSession session,
        Guid[] componentIds,
        CancellationToken cancellationToken)
    {
        if (componentIds.Length == 0)
        {
            return (ItemOutcome.Succeeded, []);
        }

        var components = new List<ItemDocument>();
        foreach (var component in await session.Events.FetchManyForWriting<ItemDocument>(componentIds, cancellationToken))
        {
            switch (component.Aggregate)
            {
                case null:
                    return (ItemOutcome.UnknownComponent, []);
                case { IsArchived: true }:
                    return (ItemOutcome.ComponentArchived, []);
                default:
                    component.AlwaysEnforceConsistency = true;
                    components.Add(component.Aggregate);
                    break;
            }
        }

        return (ItemOutcome.Succeeded, components);
    }

    /// <summary>
    /// Appends to the single item-composition stream, so concurrent component additions conflict there, then walks every
    /// item reachable from the added component (already locked) and reports whether <paramref name="itemId"/> is among
    /// them.
    /// </summary>
    /// <returns><see langword="true"/> when adding the component would make the item contain itself.</returns>
    private async Task<bool> LockCompositionAsync(
        IDocumentSession session,
        Guid itemId,
        ItemDocument added,
        CancellationToken cancellationToken)
    {
        var composition = await session.Events.FetchForWriting<ItemCompositionDocument>(ItemCompositionDocument.StreamId,
            cancellationToken);
        composition.AppendOne(new ItemCompositionChanged(itemId, timeProvider.GetUtcNow()));
        var componentsByItem = new Dictionary<Guid, IReadOnlyList<ItemComponent>> { [added.Id] = added.Components };
        Guid[] frontier =
        [
            .. added.Components.Select(component => component.ItemId).Where(id => id != itemId && id != added.Id).Distinct()
        ];
        while (frontier.Length > 0)
        {
            var streams = await session.Events.FetchManyForWriting<ItemDocument>(frontier, cancellationToken);
            for (var index = 0; index < frontier.Length; index++)
            {
                // A second guard: it rejects a component change committed before this save, even by a future writer that
                // forgets to append to the item-composition stream.
                streams[index].AlwaysEnforceConsistency = true;
                componentsByItem[frontier[index]] = streams[index].Aggregate?.Components ?? [];
            }

            frontier =
            [
                .. frontier
                    .SelectMany(id => componentsByItem[id])
                    .Select(component => component.ItemId)
                    .Where(id => id != itemId && !componentsByItem.ContainsKey(id))
                    .Distinct()
            ];
        }

        return ItemRecipes.CreatesLoop(
            itemId,
            [new ItemComponent(added.Id, 1)],
            id => componentsByItem.TryGetValue(id, out var components) ? components : []);
    }
}
