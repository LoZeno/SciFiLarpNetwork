using System.Collections.ObjectModel;
using System.Globalization;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Items;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>
/// The dialog that manages item types or rarities: create, rename, archive and unarchive, and for rarities move up or
/// down. Item types are listed by name, rarities in their order.
/// </summary>
public sealed class ItemCatalogViewModel : ScreenViewModel
{
    private readonly IItemDialogService _dialogs;
    private readonly string _href;
    private readonly AsyncCommand _reloadCommand;
    private SirenEntity? _collection;
    private ItemCatalogEntryListItem? _selectedEntry;
    private bool _showArchived;

    internal ItemCatalogViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        IItemDialogService dialogs,
        string apiBaseUrl,
        string locale,
        ItemCatalogKind kind,
        string href)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        Kind = kind;
        _href = href;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        NewCommand = new AsyncCommand(() => RunAsync(CreateAsync), HandleUnexpectedFailure, () => CanWrite && !IsBusy);
        RenameCommand = new AsyncCommand(() => RunAsync(RenameAsync), HandleUnexpectedFailure, CanChange);
        ToggleArchivedCommand = new AsyncCommand(() => RunAsync(ToggleArchivedAsync), HandleUnexpectedFailure, CanChange);
        MoveUpCommand = new AsyncCommand(() => RunAsync(() => MoveAsync("move-rarity-up")), HandleUnexpectedFailure, CanMove);
        MoveDownCommand = new AsyncCommand(() => RunAsync(() => MoveAsync("move-rarity-down")), HandleUnexpectedFailure, CanMove);
        CloseCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? CloseRequested;

    public ItemCatalogKind Kind { get; }

    public bool IsRarities => Kind == ItemCatalogKind.Rarities;

    public AsyncCommand NewCommand { get; }

    public AsyncCommand RenameCommand { get; }

    public AsyncCommand ToggleArchivedCommand { get; }

    public AsyncCommand MoveUpCommand { get; }

    public AsyncCommand MoveDownCommand { get; }

    public DelegateCommand CloseCommand { get; }

    public ObservableCollection<ItemCatalogEntryListItem> Entries { get; } = [];

    /// <summary>Whether anything was changed, so the item screen reloads its type and rarity names.</summary>
    public bool IsChanged { get; private set; }

    public ItemCatalogEntryListItem? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (_selectedEntry == value)
            {
                return;
            }

            _selectedEntry = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleArchivedLabel));
            NotifyCommandsChanged();
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
            _reloadCommand.Execute(null);
        }
    }

    public string Title => Translate(IsRarities ? "items.rarities" : "items.itemTypes");

    public string NewLabel => Translate(IsRarities ? "items.newRarity" : "items.newItemType");

    public string RenameLabel => Translate("items.rename");

    public string MoveUpLabel => Translate("items.moveUp");

    public string MoveDownLabel => Translate("items.moveDown");

    public string CloseLabel => Translate("common.close");

    public string ShowArchivedLabel => Translate("items.includeArchived");

    public string ToggleArchivedLabel => Translate(SelectedEntry is { IsArchived: true } ? "skills.unarchive" : "skills.archive");

    public string? Placeholder => Entries.Count == 0 ? Translate("common.noResults") : null;

    public bool HasPlaceholder => Placeholder is not null;

    public override Task LoadAsync() => RunAsync(LoadCoreAsync);

    private protected override void OnLocaleChanged() => _reloadCommand.Execute(null);

    private protected override void NotifyCommandsChanged()
    {
        foreach (var command in new[] { NewCommand, RenameCommand, ToggleArchivedCommand, MoveUpCommand, MoveDownCommand })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    private async Task LoadCoreAsync()
    {
        var collection = await GetAsync(_href);
        if (ShowArchived)
        {
            collection = await GetAsync(collection.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
        }

        _collection = collection;
        IsLoaded = true;
        CanWrite = collection.FindAction(CreateActionName) is not null;
        var selectedId = SelectedEntry?.Id;
        ReplaceAll(Entries, (collection.Entities ?? []).Select(ToListItem));
        SelectedEntry = Entries.FirstOrDefault(entry => entry.Id == selectedId);
        OnPropertyChanged(nameof(Placeholder));
        OnPropertyChanged(nameof(HasPlaceholder));
        NotifyCommandsChanged();
    }

    private string CreateActionName => IsRarities ? "create-rarity" : "create-item-type";

    private string RenameActionName => IsRarities ? "rename-rarity" : "rename-item-type";

    private string KindName => IsRarities ? "rarity" : "item-type";

    private async Task CreateAsync()
    {
        var action = (_collection ?? throw MissingAction()).FindAction(CreateActionName) ?? throw MissingAction();
        var editor = new ItemCatalogEntryEditorViewModel(Localizer, Locale, IsRarities ? "items.newRarity" : "items.newItemType",
            editor => SubmitEditorAsync(editor, () => SubmitAsync(action, editor.CreateBody(null)), null));
        await _dialogs.ShowCatalogEntryEditorAsync(editor);
        await ReloadAfterChangeAsync(editor.IsSaved);
    }

    private async Task RenameAsync()
    {
        var saved = await GetAsync(SelectedEntry?.Href ?? throw MissingAction());
        var editor = new ItemCatalogEntryEditorViewModel(Localizer, Locale, "items.rename",
            editor => SubmitEditorAsync(
                editor,
                async () => saved = await SubmitAsync(
                    saved.FindAction(RenameActionName) ?? throw MissingAction(),
                    editor.CreateBody(SirenProperties.Read<ItemCatalogEntryProperties>(saved.Properties).Version)),
                async () =>
                {
                    saved = await GetAsync(saved.FindLinkHref("self") ?? throw MissingAction());
                    editor.Name = SirenProperties.Read<ItemCatalogEntryProperties>(saved.Properties).Name;
                }));
        editor.Name = SirenProperties.Read<ItemCatalogEntryProperties>(saved.Properties).Name;
        await _dialogs.ShowCatalogEntryEditorAsync(editor);
        await ReloadAfterChangeAsync(editor.IsSaved);
    }

    private async Task ToggleArchivedAsync()
    {
        var entry = SelectedEntry ?? throw MissingAction();
        var resource = await GetAsync(entry.Href);
        var action = resource.FindAction(entry.IsArchived ? $"unarchive-{KindName}" : $"archive-{KindName}") ?? throw MissingAction();
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate(entry.IsArchived ? "items.confirmUnarchiveEntry" : "items.confirmArchiveEntry"), entry.Name),
            Translate(entry.IsArchived ? "skills.unarchive" : "skills.archive"),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        await SubmitAsync(action, null);
        await ReloadAfterChangeAsync(true);
    }

    private async Task MoveAsync(string actionName)
    {
        var resource = await GetAsync(SelectedEntry?.Href ?? throw MissingAction());
        await SubmitAsync(resource.FindAction(actionName) ?? throw MissingAction(), null);
        await ReloadAfterChangeAsync(true);
    }

    private async Task ReloadAfterChangeAsync(bool isChanged)
    {
        if (!isChanged)
        {
            return;
        }

        IsChanged = true;
        await LoadCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private ItemCatalogEntryListItem ToListItem(SirenSubEntity entity)
    {
        var entry = SirenProperties.Read<ItemCatalogEntryProperties>(entity.Properties);
        var name = entry.IsArchived ? $"{entry.Name} {Translate("items.archivedTag")}" : entry.Name;
        return new ItemCatalogEntryListItem(entry.Id, entry.Name, entry.IsArchived, entity.Href ?? throw MissingAction(), name);
    }

    private bool CanChange() => CanWrite && !IsBusy && SelectedEntry is not null;

    private bool CanMove() => IsRarities && CanChange();
}
