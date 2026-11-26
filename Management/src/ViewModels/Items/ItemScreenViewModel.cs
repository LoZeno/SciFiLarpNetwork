using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Items;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>
/// The items screen: a paged item list filtered by type and searched by text, the selected item's details, components
/// and printed copies, and printing. Changing the filter, the search or the archive switch starts again from the first
/// page; a slower answer for an earlier list never replaces a newer one. Every change happens in a modal dialog, so
/// moving through the list never discards typed input.
/// </summary>
public sealed class ItemScreenViewModel : ScreenViewModel
{
    private const string PdfMediaType = "application/pdf";
    private const string CardsFileName = "item-cards.pdf";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IItemDialogService _dialogs;
    private readonly string _itemsHref;
    private readonly AsyncCommand _reloadItemsCommand;
    private readonly AsyncCommand _reloadDetailsCommand;
    private SirenEntity? _collection;
    private SirenEntity? _item;
    private Dictionary<Guid, ItemCatalogEntryProperties> _types = [];
    private Dictionary<Guid, ItemCatalogEntryProperties> _rarities = [];
    private ItemListItem? _selectedItem;
    private ItemChoice? _selectedTypeFilter;
    private ItemComponentListItem? _selectedComponent;
    private ItemCopyListItem? _selectedCopy;
    private string _search = string.Empty;
    private string _copiesPerItemText = "1";
    private bool _showArchived;
    private bool _isApplyingFilters;
    private int _listGeneration;
    private int _detailsGeneration;
    private ItemProperties? _details;
    private byte[]? _detailImage;

    public ItemScreenViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        IItemDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string itemsHref)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _itemsHref = itemsHref;
        _reloadItemsCommand = new AsyncCommand(() => RunAsync(() => LoadItemsCoreAsync(null)), HandleUnexpectedFailure);
        _reloadDetailsCommand = new AsyncCommand(() => RunAsync(LoadDetailsCoreAsync), HandleUnexpectedFailure);
        SearchCommand = new AsyncCommand(() => RunAsync(() => LoadItemsCoreAsync(null)), HandleUnexpectedFailure, () => !IsBusy);
        PreviousCommand = new AsyncCommand(
            () => RunAsync(() => LoadItemsCoreAsync(_collection?.FindLinkHref(SirenRelations.Previous))),
            HandleUnexpectedFailure,
            () => !IsBusy && _collection?.FindLinkHref(SirenRelations.Previous) is not null);
        NextCommand = new AsyncCommand(
            () => RunAsync(() => LoadItemsCoreAsync(_collection?.FindLinkHref(SirenRelations.Next))),
            HandleUnexpectedFailure,
            () => !IsBusy && _collection?.FindLinkHref(SirenRelations.Next) is not null);
        NewCommand = new AsyncCommand(() => RunAsync(CreateItemAsync), HandleUnexpectedFailure, () => CanWrite && !IsBusy);
        EditCommand = new AsyncCommand(() => RunAsync(EditItemAsync), HandleUnexpectedFailure, CanChangeItem);
        CloneCommand = new AsyncCommand(() => RunAsync(CloneItemAsync), HandleUnexpectedFailure, CanChangeItem);
        ToggleArchivedCommand = new AsyncCommand(() => RunAsync(ToggleArchivedAsync), HandleUnexpectedFailure, CanChangeItem);
        AddComponentCommand = new AsyncCommand(() => RunAsync(AddComponentAsync), HandleUnexpectedFailure, CanChangeItem);
        ChangeQuantityCommand = new AsyncCommand(() => RunAsync(ChangeQuantityAsync), HandleUnexpectedFailure, CanChangeComponent);
        RemoveComponentCommand = new AsyncCommand(() => RunAsync(RemoveComponentAsync), HandleUnexpectedFailure, CanChangeComponent);
        PreviewCommand = new AsyncCommand(() => RunAsync(PreviewAsync), HandleUnexpectedFailure,
            () => CanPrint("preview-item-cards"));
        PrintCommand = new AsyncCommand(() => RunAsync(PrintAsync), HandleUnexpectedFailure, () => CanPrint("print-item-cards"));
        ReprintCommand = new AsyncCommand(() => RunAsync(ReprintAsync), HandleUnexpectedFailure,
            () => !IsBusy && SelectedCopies.Count > 0 && _collection?.FindAction("reprint-item-cards") is not null);
        ToggleCopyActiveCommand = new AsyncCommand(() => RunAsync(ToggleCopyActiveAsync), HandleUnexpectedFailure,
            () => CanWrite && !IsBusy && SelectedCopy is not null && SelectedCopies.Count <= 1);
        ManageItemTypesCommand = new AsyncCommand(() => RunAsync(() => ManageCatalogAsync(ItemCatalogKind.ItemTypes)),
            HandleUnexpectedFailure, () => !IsBusy && _collection is not null);
        ManageRaritiesCommand = new AsyncCommand(() => RunAsync(() => ManageCatalogAsync(ItemCatalogKind.Rarities)),
            HandleUnexpectedFailure, () => !IsBusy && _collection is not null);
        SelectedItems.CollectionChanged += (_, _) => NotifyCommandsChanged();
        SelectedCopies.CollectionChanged += (_, _) => NotifyCommandsChanged();
    }

    public AsyncCommand SearchCommand { get; }

    public AsyncCommand PreviousCommand { get; }

    public AsyncCommand NextCommand { get; }

    public AsyncCommand NewCommand { get; }

    public AsyncCommand EditCommand { get; }

    public AsyncCommand CloneCommand { get; }

    public AsyncCommand ToggleArchivedCommand { get; }

    public AsyncCommand AddComponentCommand { get; }

    public AsyncCommand ChangeQuantityCommand { get; }

    public AsyncCommand RemoveComponentCommand { get; }

    public AsyncCommand PreviewCommand { get; }

    public AsyncCommand PrintCommand { get; }

    public AsyncCommand ReprintCommand { get; }

    public AsyncCommand ToggleCopyActiveCommand { get; }

    public AsyncCommand ManageItemTypesCommand { get; }

    public AsyncCommand ManageRaritiesCommand { get; }

    public ObservableCollection<ItemListItem> Items { get; } = [];

    /// <summary>The items whose cards are printed, kept in step with the list's selection.</summary>
    public ObservableCollection<ItemListItem> SelectedItems { get; } = [];

    public ObservableCollection<ItemChoice> TypeFilters { get; } = [];

    public ObservableCollection<ItemComponentListItem> Components { get; } = [];

    public ObservableCollection<ItemCopyListItem> Copies { get; } = [];

    /// <summary>The copies to reprint, kept in step with the copy list's selection.</summary>
    public ObservableCollection<ItemCopyListItem> SelectedCopies { get; } = [];

    public ItemListItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value)
            {
                return;
            }

            var changed = _selectedItem?.ItemId != value?.ItemId;
            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleArchivedLabel));
            NotifyCommandsChanged();
            if (changed)
            {
                _reloadDetailsCommand.Execute(null);
            }
        }
    }

    public ItemChoice? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (_selectedTypeFilter == value)
            {
                return;
            }

            _selectedTypeFilter = value;
            OnPropertyChanged();
            if (!_isApplyingFilters)
            {
                _reloadItemsCommand.Execute(null);
            }
        }
    }

    public ItemComponentListItem? SelectedComponent
    {
        get => _selectedComponent;
        set
        {
            if (_selectedComponent == value)
            {
                return;
            }

            _selectedComponent = value;
            OnPropertyChanged();
            NotifyCommandsChanged();
        }
    }

    public ItemCopyListItem? SelectedCopy
    {
        get => _selectedCopy;
        set
        {
            if (_selectedCopy == value)
            {
                return;
            }

            _selectedCopy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleCopyActiveLabel));
            NotifyCommandsChanged();
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (_search == value)
            {
                return;
            }

            _search = value;
            OnPropertyChanged();
        }
    }

    public string CopiesPerItemText
    {
        get => _copiesPerItemText;
        set
        {
            if (_copiesPerItemText == value)
            {
                return;
            }

            _copiesPerItemText = value;
            OnPropertyChanged();
        }
    }

    public bool ShowArchived
    {
        get => _showArchived;
        set
        {
            if (_showArchived == value)
            {
                return;
            }

            _showArchived = value;
            OnPropertyChanged();
            _reloadItemsCommand.Execute(null);
        }
    }

    public bool HasDetails => _details is not null;

    public string DetailName => _details?.Name ?? string.Empty;

    public string DetailType => _details is { } item ? EntryName(_types, item.ItemTypeId) : string.Empty;

    public string DetailRarity => _details is { } item ? EntryName(_rarities, item.RarityId) : string.Empty;

    public string DetailCost => _details?.Cost.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

    public string DetailCharges => _details?.Charges?.ToString(CultureInfo.CurrentCulture) ?? Translate("items.notSet");

    public string DetailExpires =>
        _details?.ExpiresOn?.ToString("d", CultureInfo.CurrentCulture) ?? Translate("items.notSet");

    public string DetailDescription => _details?.Description ?? string.Empty;

    public string DetailEffect => _details?.Effect ?? string.Empty;

    public byte[]? DetailImage => _detailImage;

    public bool HasDetailImage => _detailImage is not null;

    public string ItemsHeading => Translate("items.items");

    public string DetailsHeading => Translate("items.details");

    public string ComponentsHeading => Translate("items.components");

    public string CopiesHeading => Translate("items.copies");

    public string SearchLabel => Translate("items.search");

    public string TypeFilterLabel => Translate("items.type");

    public string ShowArchivedLabel => Translate("items.includeArchived");

    public string PreviousLabel => Translate("players.previousPage");

    public string NextLabel => Translate("players.nextPage");

    public string NewLabel => Translate("items.new");

    public string EditLabel => Translate("skills.edit");

    public string CloneLabel => Translate("items.clone");

    public string ToggleArchivedLabel => Translate(SelectedItem is { IsArchived: true } ? "skills.unarchive" : "skills.archive");

    public string AddComponentLabel => Translate("items.addComponent");

    public string ChangeQuantityLabel => Translate("items.changeQuantity");

    public string RemoveComponentLabel => Translate("items.removeComponent");

    public string CopiesPerItemLabel => Translate("items.copiesPerItem");

    public string PreviewLabel => Translate("items.previewCards");

    public string PrintLabel => Translate("items.printCards");

    public string ReprintLabel => Translate("items.reprint");

    public string ToggleCopyActiveLabel => Translate(SelectedCopy is { IsActive: false } ? "items.reactivateCopy" : "items.deactivateCopy");

    public string ManageItemTypesLabel => Translate("items.itemTypes");

    public string ManageRaritiesLabel => Translate("items.rarities");

    public string NameLabel => Translate("items.name");

    public string RarityLabel => Translate("items.rarity");

    public string CostLabel => Translate("items.cost");

    public string ChargesLabel => Translate("items.charges");

    public string ExpiresOnLabel => Translate("items.expiresOn");

    public string DescriptionLabel => Translate("items.description");

    public string EffectLabel => Translate("items.effect");

    public string PageText { get; private set; } = string.Empty;

    public string? ItemsPlaceholder => Items.Count == 0 ? Translate("common.noResults") : null;

    public bool HasItemsPlaceholder => ItemsPlaceholder is not null;

    public string? DetailsPlaceholder => HasDetails ? null : Translate("items.selectItem");

    public bool HasDetailsPlaceholder => DetailsPlaceholder is not null;

    public override Task LoadAsync() => RunAsync(() => LoadItemsCoreAsync(null));

    private protected override void OnLocaleChanged()
    {
        foreach (var property in typeof(ItemScreenViewModel).GetProperties().Where(property => property.Name.EndsWith("Label", StringComparison.Ordinal)
                     || property.Name.EndsWith("Heading", StringComparison.Ordinal)))
        {
            OnPropertyChanged(property.Name);
        }

        _reloadItemsCommand.Execute(null);
    }

    private protected override void NotifyCommandsChanged()
    {
        foreach (var command in new[]
                 {
                     SearchCommand, PreviousCommand, NextCommand, NewCommand, EditCommand, CloneCommand, ToggleArchivedCommand,
                     AddComponentCommand, ChangeQuantityCommand, RemoveComponentCommand, PreviewCommand, PrintCommand, ReprintCommand,
                     ToggleCopyActiveCommand, ManageItemTypesCommand, ManageRaritiesCommand
                 })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Loads <paramref name="href"/>, or the first page matching the current type, search text and archive switch.</summary>
    private async Task LoadItemsCoreAsync(string? href)
    {
        var generation = ++_listGeneration;
        var collection = await GetAsync(href ?? _itemsHref);
        if (href is null && (!string.IsNullOrWhiteSpace(Search) || ShowArchived || SelectedTypeFilter?.Id is not null))
        {
            var search = collection.FindAction("search-items") ?? throw MissingAction();
            collection = await GetAsync(search.ToQueryHref(new Dictionary<string, string?>
            {
                ["search"] = Search,
                ["itemTypeId"] = SelectedTypeFilter?.Id?.ToString("D"),
                ["includeArchived"] = ShowArchived ? "true" : null
            }));
        }

        var types = await CatalogAsync(collection, SirenRelations.ItemTypes);
        var rarities = await CatalogAsync(collection, SirenRelations.Rarities);
        if (generation != _listGeneration)
        {
            return;
        }

        _collection = collection;
        _types = types;
        _rarities = rarities;
        IsLoaded = true;
        CanWrite = collection.FindAction("create-item") is not null;
        ApplyTypeFilters();
        var page = SirenProperties.Read<ItemCollectionProperties>(collection.Properties);
        PageText = string.Format(CultureInfo.CurrentCulture, Translate("players.page"), page.PageNumber, page.PageCount, page.TotalCount);
        OnPropertyChanged(nameof(PageText));
        var selectedId = SelectedItem?.ItemId;
        ReplaceAll(Items, (collection.Entities ?? []).Select(ToListItem));
        SelectedItem = Items.FirstOrDefault(item => item.ItemId == selectedId);
        KeepOnlySelectedItemForPrinting();
        OnPropertyChanged(nameof(ItemsPlaceholder));
        OnPropertyChanged(nameof(HasItemsPlaceholder));
        NotifyCommandsChanged();
        await LoadDetailsCoreAsync();
    }

    private async Task<Dictionary<Guid, ItemCatalogEntryProperties>> CatalogAsync(SirenEntity collection, string rel)
    {
        var catalog = await GetAsync(collection.FindLinkHref(rel) ?? throw MissingAction());
        catalog = await GetAsync(catalog.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
        return (catalog.Entities ?? [])
            .Select(entity => SirenProperties.Read<ItemCatalogEntryProperties>(entity.Properties))
            .ToDictionary(entry => entry.Id);
    }

    private void ApplyTypeFilters()
    {
        var selectedId = SelectedTypeFilter?.Id;
        _isApplyingFilters = true;
        try
        {
            ReplaceAll(TypeFilters,
            [
                new ItemChoice(null, Translate("items.allTypes")),
                .. _types.Values.Where(type => !type.IsArchived || type.Id == selectedId)
                    .OrderBy(type => type.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(type => new ItemChoice(type.Id, type.Name))
            ]);
            SelectedTypeFilter = TypeFilters.FirstOrDefault(choice => choice.Id == selectedId) ?? TypeFilters[0];
        }
        finally
        {
            _isApplyingFilters = false;
        }
    }

    /// <summary>Loads the selected item, its picture and its copies; clears them when nothing is selected.</summary>
    private async Task LoadDetailsCoreAsync()
    {
        var generation = ++_detailsGeneration;
        SirenEntity? item = null;
        SirenEntity? copies = null;
        byte[]? image = null;
        if (SelectedItem is { } selected)
        {
            item = await GetAsync(selected.Href);
            copies = await GetAsync(item.FindLinkHref(SirenRelations.ItemCopies) ?? throw MissingAction());
            image = await TryGetImageAsync(item);
        }

        if (generation != _detailsGeneration)
        {
            return;
        }

        _item = item;
        _details = item is null ? null : SirenProperties.Read<ItemProperties>(item.Properties);
        _detailImage = image;
        ReplaceAll(Components, (_details?.Components ?? []).Select(ToComponentItem));
        SelectedComponent = null;
        var selectedCopyId = SelectedCopy?.CopyId;
        ReplaceAll(Copies, (copies?.Entities ?? []).Select(ToCopyItem));
        SelectedCopy = Copies.FirstOrDefault(copy => copy.CopyId == selectedCopyId);
        SelectedCopies.Clear();
        if (SelectedCopy is { } restored)
        {
            SelectedCopies.Add(restored);
        }

        foreach (var name in new[]
                 {
                     nameof(HasDetails), nameof(DetailName), nameof(DetailType), nameof(DetailRarity), nameof(DetailCost),
                     nameof(DetailCharges), nameof(DetailExpires), nameof(DetailDescription), nameof(DetailEffect),
                     nameof(DetailImage), nameof(HasDetailImage), nameof(DetailsPlaceholder), nameof(HasDetailsPlaceholder)
                 })
        {
            OnPropertyChanged(name);
        }

        NotifyCommandsChanged();
    }

    /// <summary>A picture that cannot be downloaded is not shown; the rest of the item still is.</summary>
    private async Task<byte[]?> TryGetImageAsync(SirenEntity item)
    {
        if (item.FindLinkHref(SirenRelations.ItemImage) is not { } href)
        {
            return null;
        }

        try
        {
            return await ApiClient.GetImageAsync(ApiBaseUrl, href, Locale);
        }
        catch (Exception failure) when (IsExpectedFailure(failure))
        {
            return null;
        }
    }

    private async Task CreateItemAsync()
    {
        var action = (_collection ?? throw MissingAction()).FindAction("create-item") ?? throw MissingAction();
        SirenEntity? saved = null;
        var editor = NewEditor("items.newItem", isClone: false, null, editor => SubmitEditorAsync(
            editor,
            async () =>
            {
                saved = saved is null
                    ? await SubmitAsync(action, editor.CreateBody(null, null, null))
                    : await UpdateDetailsAsync(saved, editor);
                saved = await SaveImageChangeAsync(saved, editor);
            },
            null));
        await _dialogs.ShowItemEditorAsync(editor);
        await ReloadAfterSaveAsync(editor.IsSaved, saved);
    }

    private async Task EditItemAsync()
    {
        var saved = await GetAsync(SelectedItem?.Href ?? throw MissingAction());
        var item = SirenProperties.Read<ItemProperties>(saved.Properties);
        var editor = NewEditor("items.editItem", isClone: false, item, editor => SubmitEditorAsync(
            editor,
            async () =>
            {
                saved = await UpdateDetailsAsync(saved, editor);
                saved = await SaveImageChangeAsync(saved, editor);
            },
            async () =>
            {
                saved = await GetAsync(saved.FindLinkHref("self") ?? throw MissingAction());
                editor.Apply(SirenProperties.Read<ItemProperties>(saved.Properties));
            }));
        editor.Apply(item);
        editor.ShowSavedImage(await TryGetImageAsync(saved));
        await _dialogs.ShowItemEditorAsync(editor);
        await ReloadAfterSaveAsync(editor.IsSaved, saved);
    }

    /// <summary>
    /// Opens the dialog filled with the item's fields; saving creates a new item with the same components and, unless
    /// the picture is changed, the same picture.
    /// </summary>
    private async Task CloneItemAsync()
    {
        var action = (_collection ?? throw MissingAction()).FindAction("create-item") ?? throw MissingAction();
        var sourceResource = await GetAsync(SelectedItem?.Href ?? throw MissingAction());
        var source = SirenProperties.Read<ItemProperties>(sourceResource.Properties);
        SirenEntity? saved = null;
        var editor = NewEditor("items.cloneItem", isClone: true, source, editor => SubmitEditorAsync(
            editor,
            async () =>
            {
                saved = saved is null
                    ? await SubmitAsync(action, editor.CreateBody(null, source.Components, source.ItemId))
                    : await UpdateDetailsAsync(saved, editor);
                saved = await SaveImageChangeAsync(saved, editor);
            },
            null));
        editor.Apply(source);
        editor.ShowSavedImage(await TryGetImageAsync(sourceResource));
        await _dialogs.ShowItemEditorAsync(editor);
        await ReloadAfterSaveAsync(editor.IsSaved, saved);
    }

    private ItemEditorViewModel NewEditor(string titleKey, bool isClone, ItemProperties? current,
        Func<ItemEditorViewModel, Task<bool>> submit)
    {
        var limits = SirenProperties.Read<ItemCollectionProperties>((_collection ?? throw MissingAction()).Properties);
        return new ItemEditorViewModel(
            Localizer,
            Locale,
            titleKey,
            Choices(_types, current?.ItemTypeId),
            Choices(_rarities, current?.RarityId),
            isClone,
            limits.MaximumImageByteCount,
            _dialogs.PickImageAsync,
            submit);
    }

    /// <summary>Entries that are not archived, plus the item's own entry when it was archived since.</summary>
    private static ItemChoice[] Choices(Dictionary<Guid, ItemCatalogEntryProperties> entries, Guid? currentId) =>
        [
            .. entries.Values
                .Where(entry => !entry.IsArchived || entry.Id == currentId)
                .OrderBy(entry => entry.Position)
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(entry => new ItemChoice(entry.Id, entry.Name))
        ];

    private Task<SirenEntity> UpdateDetailsAsync(SirenEntity saved, ItemEditorViewModel editor) =>
        SubmitAsync(
            saved.FindAction("update-item") ?? throw MissingAction(),
            editor.CreateBody(SirenProperties.Read<ItemProperties>(saved.Properties).Version, null, null));

    /// <summary>Sends the picture change the dialog holds, using the actions and version of the item just saved.</summary>
    private async Task<SirenEntity> SaveImageChangeAsync(SirenEntity saved, ItemEditorViewModel editor)
    {
        var actionName = editor.ImageChange switch
        {
            ItemImageChange.Upload => "set-item-image",
            ItemImageChange.Remove => "remove-item-image",
            _ => null
        };
        if (actionName is null)
        {
            return saved;
        }

        if (saved.FindAction(actionName) is not { } action)
        {
            if (editor.ImageChange == ItemImageChange.Remove)
            {
                editor.MarkImageChangeSaved();
                return saved;
            }

            throw MissingAction();
        }

        var result = await SubmitAsync(action, editor.CreateImageBody(SirenProperties.Read<ItemProperties>(saved.Properties).Version));
        editor.MarkImageChangeSaved();
        return result;
    }

    private async Task ReloadAfterSaveAsync(bool isSaved, SirenEntity? saved)
    {
        if (!isSaved)
        {
            return;
        }

        await LoadItemsCoreAsync(_collection?.FindLinkHref("self"));
        if (saved is not null && Items.FirstOrDefault(item => item.Href == saved.FindLinkHref("self")) is { } shown)
        {
            SelectedItem = shown;
            KeepOnlySelectedItemForPrinting();
        }

        StatusMessage = Translate("actions.completed");
    }

    private async Task ToggleArchivedAsync()
    {
        var item = SelectedItem ?? throw MissingAction();
        var resource = await GetAsync(item.Href);
        var action = resource.FindAction(item.IsArchived ? "unarchive-item" : "archive-item") ?? throw MissingAction();
        if (await ConfirmAsync(item.IsArchived ? "items.confirmUnarchive" : "items.confirmArchive",
                item.IsArchived ? "skills.unarchive" : "skills.archive", item.Name))
        {
            await SubmitAsync(action, null);
            await LoadItemsCoreAsync(_collection?.FindLinkHref("self"));
            StatusMessage = Translate("actions.completed");
        }
    }

    private async Task AddComponentAsync()
    {
        var item = await GetAsync(SelectedItem?.Href ?? throw MissingAction());
        var action = item.FindAction("add-item-component") ?? throw MissingAction();
        var version = SirenProperties.Read<ItemProperties>(item.Properties).Version;
        var editor = new ItemComponentEditorViewModel(Localizer, Locale, "items.addComponent", null, 1, SearchComponentsAsync,
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateAddBody(version)), null));
        await editor.SearchCommand.ExecuteAsync();
        await _dialogs.ShowComponentEditorAsync(editor);
        await ReloadDetailsAfterChangeAsync(editor.IsSaved);
    }

    /// <summary>The items that can become components: not archived, and not the item itself.</summary>
    private async Task<IReadOnlyList<ItemChoice>> SearchComponentsAsync(string text)
    {
        var search = (_collection ?? throw MissingAction()).FindAction("search-items") ?? throw MissingAction();
        var found = await GetAsync(search.ToQueryHref(new Dictionary<string, string?> { ["search"] = text }));
        return
        [
            .. (found.Entities ?? [])
                .Select(entity => SirenProperties.Read<ItemProperties>(entity.Properties))
                .Where(item => item.ItemId != SelectedItem?.ItemId)
                .Select(item => new ItemChoice(item.ItemId, item.Name))
        ];
    }

    private async Task ChangeQuantityAsync()
    {
        var component = SelectedComponent ?? throw MissingAction();
        var action = ComponentAction(component, "change-component-quantity");
        var version = SirenProperties.Read<ItemProperties>((_item ?? throw MissingAction()).Properties).Version;
        var editor = new ItemComponentEditorViewModel(Localizer, Locale, "items.changeQuantity", component.Name, component.Quantity, null,
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateQuantityBody(version)), null));
        await _dialogs.ShowComponentEditorAsync(editor);
        await ReloadDetailsAfterChangeAsync(editor.IsSaved);
    }

    private async Task RemoveComponentAsync()
    {
        var component = SelectedComponent ?? throw MissingAction();
        var action = ComponentAction(component, "remove-item-component");
        var version = SirenProperties.Read<ItemProperties>((_item ?? throw MissingAction()).Properties).Version;
        if (await ConfirmAsync("items.confirmRemoveComponent", "items.removeComponent", component.Name))
        {
            await SubmitAsync(action, JsonSerializer.SerializeToElement(new { version }, JsonOptions));
            await ReloadDetailsAfterChangeAsync(true);
        }
    }

    private SirenAction ComponentAction(ItemComponentListItem component, string name) =>
        (_item?.Entities ?? [])
        .FirstOrDefault(entity => entity.Properties is { } properties
                                  && SirenProperties.Read<ItemComponentProperties>(properties).ItemId == component.ItemId)
        ?.FindAction(name) ?? throw MissingAction();

    private async Task ReloadDetailsAfterChangeAsync(bool isChanged)
    {
        if (!isChanged)
        {
            return;
        }

        await LoadDetailsCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private async Task PreviewAsync()
    {
        if (CopiesPerItem() is not { } copies)
        {
            return;
        }

        var pdf = await DownloadAsync("preview-item-cards", new { itemIds = SelectedItemIds(), copiesPerItem = copies });
        if (!await _dialogs.OpenPdfAsync(pdf, CardsFileName))
        {
            StatusMessage = Translate("players.cardsPreviewFailed");
        }
    }

    /// <summary>
    /// Makes the copies and opens their cards. A new print identifier is chosen for each click, so a retry after a lost
    /// answer is a new print; the copies of a print whose cards could not be opened can be reprinted from the copy list.
    /// </summary>
    private async Task PrintAsync()
    {
        if (CopiesPerItem() is not { } copies)
        {
            return;
        }

        var itemIds = SelectedItemIds();
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate("items.confirmPrint"), itemIds.Length * copies),
            Translate("items.printCards"),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        var pdf = await DownloadAsync("print-item-cards", new { printId = Guid.NewGuid(), itemIds, copiesPerItem = copies });
        await LoadDetailsCoreAsync();
        StatusMessage = await _dialogs.OpenPdfAsync(pdf, CardsFileName)
            ? Translate("items.printed")
            : Translate("items.printedNotOpened");
    }

    private async Task ReprintAsync()
    {
        var copyIds = Copies.Where(SelectedCopies.Contains).Select(copy => copy.CopyId).ToArray();
        var pdf = await DownloadAsync("reprint-item-cards", new { copyIds });
        if (!await _dialogs.OpenPdfAsync(pdf, CardsFileName))
        {
            StatusMessage = Translate("players.cardsPreviewFailed");
        }
    }

    private Task<byte[]> DownloadAsync(string actionName, object body) =>
        ApiClient.DownloadActionAsync(
            ApiBaseUrl,
            (_collection ?? throw MissingAction()).FindAction(actionName) ?? throw MissingAction(),
            JsonSerializer.SerializeToElement(body, JsonOptions),
            PdfMediaType,
            Locale);

    private Guid[] SelectedItemIds() => [.. Items.Where(SelectedItems.Contains).Select(item => item.ItemId)];

    /// <summary>The typed number of copies, or <see langword="null"/> after showing why it is not valid.</summary>
    private int? CopiesPerItem()
    {
        var limit = SirenProperties.Read<ItemCollectionProperties>((_collection ?? throw MissingAction()).Properties).MaximumCopiesPerItem;
        if (int.TryParse(CopiesPerItemText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var copies)
            && copies >= 1 && copies <= limit)
        {
            return copies;
        }

        StatusMessage = Localizer.TranslateApiError("errors.invalidCopyCount", Locale);
        return null;
    }

    private async Task ToggleCopyActiveAsync()
    {
        var copy = SelectedCopy ?? throw MissingAction();
        var copies = await GetAsync((_item ?? throw MissingAction()).FindLinkHref(SirenRelations.ItemCopies) ?? throw MissingAction());
        var entity = (copies.Entities ?? []).FirstOrDefault(candidate =>
                         SirenProperties.Read<ItemCopyProperties>(candidate.Properties).CopyId == copy.CopyId)
                     ?? throw MissingAction();
        var action = entity.FindAction(copy.IsActive ? "deactivate-item-copy" : "reactivate-item-copy") ?? throw MissingAction();
        if (await ConfirmAsync(copy.IsActive ? "items.confirmDeactivateCopy" : "items.confirmReactivateCopy",
                copy.IsActive ? "items.deactivateCopy" : "items.reactivateCopy", copy.QrCode))
        {
            await SubmitAsync(action, null);
            await ReloadDetailsAfterChangeAsync(true);
        }
    }

    private async Task ManageCatalogAsync(ItemCatalogKind kind)
    {
        var href = (_collection ?? throw MissingAction()).FindLinkHref(
            kind == ItemCatalogKind.Rarities ? SirenRelations.Rarities : SirenRelations.ItemTypes) ?? throw MissingAction();
        var catalog = new ItemCatalogViewModel(ApiClient, Localizer, _dialogs, ApiBaseUrl, Locale, kind, href);
        catalog.SessionExpired += (_, _) =>
            StatusMessage = Report(new ManagementApiException("errors.unauthorized", System.Net.HttpStatusCode.Unauthorized));
        await catalog.LoadAsync();
        await _dialogs.ShowCatalogAsync(catalog);
        if (catalog.IsChanged)
        {
            await LoadItemsCoreAsync(_collection?.FindLinkHref("self"));
        }
    }

    private Task<bool> ConfirmAsync(string messageKey, string confirmKey, string name) =>
        _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate(messageKey), name),
            Translate(confirmKey),
            Translate("common.cancel")));

    private ItemListItem ToListItem(SirenSubEntity entity)
    {
        var item = SirenProperties.Read<ItemProperties>(entity.Properties);
        var name = item.IsArchived ? $"{item.Name} {Translate("items.archivedTag")}" : item.Name;
        return new ItemListItem(item.ItemId, item.Name, item.IsArchived, entity.Href ?? throw MissingAction(),
            $"{name} — {EntryName(_types, item.ItemTypeId)}");
    }

    private ItemComponentListItem ToComponentItem(ItemComponentProperties component)
    {
        var name = component.IsArchived ? $"{component.Name} {Translate("items.archivedTag")}" : component.Name;
        return new ItemComponentListItem(component.ItemId, component.Name, component.Quantity, component.IsArchived,
            $"{component.Quantity} × {name}");
    }

    private ItemCopyListItem ToCopyItem(SirenSubEntity entity)
    {
        var copy = SirenProperties.Read<ItemCopyProperties>(entity.Properties);
        var state = Translate(copy.IsActive ? "items.activeTag" : "items.deactivatedTag");
        var printed = string.Format(CultureInfo.CurrentCulture, Translate("items.printedBy"),
            copy.PrintedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture), copy.PrintedBy ?? Translate("items.unknownAdministrator"));
        return new ItemCopyListItem(copy.CopyId, copy.QrCode, copy.IsActive, $"{copy.QrCode} {state} — {printed}");
    }

    private string EntryName(Dictionary<Guid, ItemCatalogEntryProperties> entries, Guid id) =>
        entries.TryGetValue(id, out var entry)
            ? entry.IsArchived ? $"{entry.Name} {Translate("items.archivedTag")}" : entry.Name
            : Translate("items.unknownEntry");

    // Clearing the list resets the ListBox selection, so after a reload only the restored row stays selected.
    private void KeepOnlySelectedItemForPrinting()
    {
        foreach (var other in SelectedItems.Where(item => item != SelectedItem).ToArray())
        {
            SelectedItems.Remove(other);
        }

        if (SelectedItem is { } selected && !SelectedItems.Contains(selected))
        {
            SelectedItems.Add(selected);
        }
    }

    private bool CanPrint(string actionName) =>
        !IsBusy && SelectedItems.Count > 0 && _collection?.FindAction(actionName) is not null;

    // Edit, clone, archive and components act on one item, so they are off while several are selected.
    private bool CanChangeItem() => CanWrite && !IsBusy && SelectedItem is not null && SelectedItems.Count <= 1 && _item is not null;

    private bool CanChangeComponent() => CanChangeItem() && SelectedComponent is not null;
}
