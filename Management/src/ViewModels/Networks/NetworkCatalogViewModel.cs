using System.Collections.ObjectModel;
using System.Globalization;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Networks;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Networks;

/// <summary>
/// The networks screen: one list with a search box, a disabled filter, and the default-network warning.
/// </summary>
public sealed class NetworkCatalogViewModel : ScreenViewModel
{
    private readonly INetworkDialogService _dialogs;
    private readonly string _networksHref;
    private readonly AsyncCommand _reloadCommand;
    private NetworkListItem[] _allNetworks = [];
    private SirenEntity? _collection;
    private NetworkListItem? _selectedNetwork;
    private string _searchText = string.Empty;
    private bool _showDisabled;
    private bool _hasDefault = true;
    private int _loadGeneration;

    public NetworkCatalogViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        INetworkDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string networksHref)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _networksHref = networksHref;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        NewNetworkCommand = new AsyncCommand(() => RunAsync(CreateNetworkAsync), HandleUnexpectedFailure, () => CanWrite && !IsBusy);
        EditNetworkCommand = new AsyncCommand(() => RunAsync(EditNetworkAsync), HandleUnexpectedFailure, CanChangeSelected);
        ToggleEnabledCommand = new AsyncCommand(
            () => RunAsync(ToggleEnabledAsync),
            HandleUnexpectedFailure,
            () => CanChangeSelected() && SelectedNetwork is not { IsDefault: true });
        MakeDefaultCommand = new AsyncCommand(
            () => RunAsync(MakeDefaultAsync),
            HandleUnexpectedFailure,
            () => CanChangeSelected() && SelectedNetwork is { IsEnabled: true, IsDefault: false });
    }

    public AsyncCommand NewNetworkCommand { get; }

    public AsyncCommand EditNetworkCommand { get; }

    public AsyncCommand ToggleEnabledCommand { get; }

    public AsyncCommand MakeDefaultCommand { get; }

    /// <summary>The networks that match the search text.</summary>
    public ObservableCollection<NetworkListItem> Networks { get; } = [];

    public NetworkListItem? SelectedNetwork
    {
        get => _selectedNetwork;
        set
        {
            if (_selectedNetwork == value)
            {
                return;
            }

            _selectedNetwork = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleEnabledLabel));
            NotifyCommandsChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            ApplySearch();
        }
    }

    public bool ShowDisabled
    {
        get => _showDisabled;
        set
        {
            if (_showDisabled == value)
            {
                return;
            }

            _showDisabled = value;
            OnPropertyChanged();
            _reloadCommand.Execute(null);
        }
    }

    /// <summary>Whether the API reported an enabled default network.</summary>
    public bool HasDefault
    {
        get => _hasDefault;
        private set
        {
            if (_hasDefault == value)
            {
                return;
            }

            _hasDefault = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoDefaultWarning));
        }
    }

    public bool HasNoDefaultWarning => IsLoaded && !HasDefault;

    public string NetworksHeading => Translate("networks.networks");

    public string SearchLabel => Translate("networks.search");

    public string ShowDisabledLabel => Translate("networks.showDisabled");

    public string NoDefaultWarning => Translate("networks.noDefaultWarning");

    public string NewLabel => Translate("skills.new");

    public string EditLabel => Translate("skills.edit");

    public string MakeDefaultLabel => Translate("networks.makeDefault");

    public string ToggleEnabledLabel => Translate(SelectedNetwork is { IsEnabled: false } ? "networks.enable" : "networks.disable");

    public string? NetworksPlaceholder => Networks.Count == 0 ? Translate("common.noResults") : null;

    public bool HasNetworksPlaceholder => NetworksPlaceholder is not null;

    public override Task LoadAsync() => RunAsync(LoadNetworksCoreAsync);

    private protected override void OnLocaleChanged()
    {
        OnPropertyChanged(nameof(NetworksHeading));
        OnPropertyChanged(nameof(SearchLabel));
        OnPropertyChanged(nameof(ShowDisabledLabel));
        OnPropertyChanged(nameof(NoDefaultWarning));
        OnPropertyChanged(nameof(NewLabel));
        OnPropertyChanged(nameof(EditLabel));
        OnPropertyChanged(nameof(MakeDefaultLabel));
        OnPropertyChanged(nameof(ToggleEnabledLabel));
        OnPropertyChanged(nameof(NetworksPlaceholder));
        _reloadCommand.Execute(null);
    }

    private protected override void NotifyCommandsChanged()
    {
        NewNetworkCommand.NotifyCanExecuteChanged();
        EditNetworkCommand.NotifyCanExecuteChanged();
        ToggleEnabledCommand.NotifyCanExecuteChanged();
        MakeDefaultCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadNetworksCoreAsync()
    {
        var generation = ++_loadGeneration;
        var collection = await GetAsync(_networksHref);
        if (ShowDisabled)
        {
            collection = await GetAsync(collection.FindLinkHref(SirenRelations.WithDisabled) ?? throw MissingAction());
        }

        if (generation != _loadGeneration)
        {
            return;
        }

        _collection = collection;
        IsLoaded = true;
        CanWrite = collection.FindAction("create-network") is not null;
        HasDefault = SirenProperties.Read<NetworkCollectionProperties>(collection.Properties).HasDefault;
        OnPropertyChanged(nameof(HasNoDefaultWarning));
        _allNetworks = (collection.Entities ?? []).Select(ToListItem).ToArray();
        ApplySearch();
    }

    private void ApplySearch()
    {
        var selectedId = SelectedNetwork?.NetworkId;
        var search = SearchText.Trim();
        ReplaceAll(Networks, _allNetworks.Where(network =>
            search.Length == 0 ||
            network.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
            network.Code.Contains(search, StringComparison.OrdinalIgnoreCase)));
        SelectedNetwork = Networks.FirstOrDefault(network => network.NetworkId == selectedId);
        OnPropertyChanged(nameof(NetworksPlaceholder));
        OnPropertyChanged(nameof(HasNetworksPlaceholder));
    }

    private async Task CreateNetworkAsync()
    {
        var collection = _collection ?? throw MissingAction();
        var createAction = collection.FindAction("create-network") ?? throw MissingAction();
        SirenEntity? saved = null;
        var editor = new NetworkEditorViewModel(
            Localizer,
            Locale,
            "networks.newNetwork",
            Themes(collection),
            isCodeEditable: true,
            _dialogs.PickImageAsync,
            editor => SubmitEditorAsync(
                editor,
                async () =>
                {
                    saved = saved is null
                        ? await SubmitAsync(createAction, editor.CreateDetailsBody(null))
                        : await UpdateDetailsAsync(saved, editor);
                    saved = await SaveImageChangeAsync(saved, editor);
                },
                () => saved is null ? Task.CompletedTask : ReloadIntoEditorAsync(saved, editor, entity => saved = entity)));
        await _dialogs.ShowNetworkEditorAsync(editor);
        await ReloadAfterSaveAsync(editor, saved);
    }

    private async Task EditNetworkAsync()
    {
        var href = SelectedNetwork?.Href ?? throw MissingAction();
        var collection = _collection ?? throw MissingAction();
        var saved = await GetAsync(href);
        var editor = new NetworkEditorViewModel(
            Localizer,
            Locale,
            "networks.editNetwork",
            Themes(collection),
            isCodeEditable: false,
            _dialogs.PickImageAsync,
            editor => SubmitEditorAsync(
                editor,
                async () =>
                {
                    saved = await UpdateDetailsAsync(saved, editor);
                    saved = await SaveImageChangeAsync(saved, editor);
                },
                () => ReloadIntoEditorAsync(saved, editor, entity => saved = entity)));
        await ApplyAsync(editor, saved);
        await _dialogs.ShowNetworkEditorAsync(editor);
        await ReloadAfterSaveAsync(editor, saved);
    }

    private Task<SirenEntity> UpdateDetailsAsync(SirenEntity saved, NetworkEditorViewModel editor) =>
        SubmitAsync(
            saved.FindAction("update-network") ?? throw MissingAction(),
            editor.CreateDetailsBody(SirenProperties.Read<NetworkProperties>(saved.Properties).Version));

    /// <summary>Sends the image change the dialog holds, using the actions and version of the network just saved.</summary>
    private async Task<SirenEntity> SaveImageChangeAsync(SirenEntity saved, NetworkEditorViewModel editor)
    {
        var actionName = editor.ImageChange switch
        {
            NetworkImageChange.Upload => "set-background-image",
            NetworkImageChange.Remove => "remove-background-image",
            _ => null
        };
        if (actionName is null)
        {
            return saved;
        }

        if (saved.FindAction(actionName) is not { } action)
        {
            if (editor.ImageChange == NetworkImageChange.Remove)
            {
                editor.MarkImageChangeSaved();
                return saved;
            }

            throw MissingAction();
        }

        var version = SirenProperties.Read<NetworkProperties>(saved.Properties).Version;
        var result = await SubmitAsync(action, editor.CreateImageBody(version));
        editor.MarkImageChangeSaved();
        return result;
    }

    private async Task ReloadIntoEditorAsync(SirenEntity saved, NetworkEditorViewModel editor, Action<SirenEntity> store)
    {
        var latest = await GetAsync(saved.FindLinkHref("self") ?? throw MissingAction());
        store(latest);
        await ApplyAsync(editor, latest);
    }

    private async Task ApplyAsync(NetworkEditorViewModel editor, SirenEntity network)
    {
        var properties = SirenProperties.Read<NetworkProperties>(network.Properties);
        editor.Code = properties.Code;
        editor.Name = properties.Name;
        editor.Description = properties.Description ?? string.Empty;
        editor.SelectTheme(properties.Theme);
        editor.ShowSavedImage(network.FindLinkHref(SirenRelations.BackgroundImage) is { } imageHref
            ? await ApiClient.GetImageAsync(ApiBaseUrl, imageHref, Locale)
            : null);
    }

    private async Task ReloadAfterSaveAsync(NetworkEditorViewModel editor, SirenEntity? saved)
    {
        if (saved is null && !editor.IsSaved)
        {
            return;
        }

        await LoadNetworksCoreAsync();
        if (editor.IsSaved)
        {
            StatusMessage = Translate("actions.completed");
        }
    }

    private async Task ToggleEnabledAsync()
    {
        var network = SelectedNetwork ?? throw MissingAction();
        var verb = network.IsEnabled ? "disable" : "enable";
        await ConfirmAndSubmitAsync(
            network,
            $"{verb}-network",
            network.IsEnabled ? "networks.confirmDisable" : "networks.confirmEnable",
            $"networks.{verb}");
    }

    private Task MakeDefaultAsync() =>
        ConfirmAndSubmitAsync(
            SelectedNetwork ?? throw MissingAction(),
            "make-default-network",
            "networks.confirmMakeDefault",
            "networks.makeDefault");

    private async Task ConfirmAndSubmitAsync(NetworkListItem network, string actionName, string messageKey, string confirmKey)
    {
        var resource = await GetAsync(network.Href);
        var action = resource.FindAction(actionName) ?? throw MissingAction();
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate(messageKey), network.Name),
            Translate(confirmKey),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        await SubmitAsync(action, null);
        await LoadNetworksCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private static NetworkThemeChoice[] Themes(SirenEntity collection) =>
        SirenProperties.Read<NetworkCollectionProperties>(collection.Properties).Themes
            .Select(theme => new NetworkThemeChoice(theme.Name, theme.Title))
            .ToArray();

    private NetworkListItem ToListItem(SirenSubEntity entity)
    {
        var network = SirenProperties.Read<NetworkProperties>(entity.Properties);
        var tags = new List<string>();
        if (network.IsDefault)
        {
            tags.Add(Translate("networks.defaultTag"));
        }

        if (!network.IsEnabled)
        {
            tags.Add(Translate("networks.disabledTag"));
        }

        return new NetworkListItem(
            network.NetworkId,
            network.Code,
            network.Name,
            network.IsEnabled,
            network.IsDefault,
            entity.Href ?? throw MissingAction(),
            string.Join(' ', [$"{network.Name} [{network.Code}]", .. tags]));
    }

    private bool CanChangeSelected() => CanWrite && !IsBusy && SelectedNetwork is not null;
}
