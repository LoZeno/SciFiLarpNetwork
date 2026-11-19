using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.Networks;
using SciFiLarpNetwork.Management.Api.Players;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>
/// The players screen: a paged, searchable player list, and the selected player's characters. The character list
/// can instead show a search across every character, or the character found by a QR identifier.
/// </summary>
public sealed class PlayerRosterViewModel : ScreenViewModel
{
    private const string ActiveStatus = "active";
    private const string PrintCardsAction = "print-character-cards";
    private const string PdfMediaType = "application/pdf";
    private const string CardsFileName = "character-cards.pdf";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IPlayerDialogService _dialogs;
    private readonly string _playersHref;
    private readonly AsyncCommand _reloadPlayersCommand;
    private readonly AsyncCommand _reloadCharactersCommand;
    private SirenEntity? _players;
    private SirenEntity? _selectedPlayerResource;
    private SirenEntity? _characterResults;
    private PlayerListItem? _selectedPlayer;
    private CharacterListItem? _selectedCharacter;
    private string _playerSearch = string.Empty;
    private string _characterSearch = string.Empty;
    private string _qrIdentifier = string.Empty;
    private bool _showArchivedPlayers;
    private bool _showArchivedCharacters;
    private bool _isCharacterSearchActive;
    private int _playerLoadGeneration;
    private int _characterLoadGeneration;

    public PlayerRosterViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        IPlayerDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string playersHref)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _playersHref = playersHref;
        _reloadPlayersCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        _reloadCharactersCommand = new AsyncCommand(() => RunAsync(LoadCharactersCoreAsync), HandleUnexpectedFailure);
        SearchPlayersCommand = new AsyncCommand(() => RunAsync(() => LoadPlayersCoreAsync(null)), HandleUnexpectedFailure, () => !IsBusy);
        PreviousPlayersCommand = new AsyncCommand(
            () => RunAsync(() => LoadPlayersCoreAsync(_players?.FindLinkHref(SirenRelations.Previous))),
            HandleUnexpectedFailure,
            () => !IsBusy && _players?.FindLinkHref(SirenRelations.Previous) is not null);
        NextPlayersCommand = new AsyncCommand(
            () => RunAsync(() => LoadPlayersCoreAsync(_players?.FindLinkHref(SirenRelations.Next))),
            HandleUnexpectedFailure,
            () => !IsBusy && _players?.FindLinkHref(SirenRelations.Next) is not null);
        NewPlayerCommand = new AsyncCommand(() => RunAsync(CreatePlayerAsync), HandleUnexpectedFailure, () => CanWrite && !IsBusy);
        EditPlayerCommand = new AsyncCommand(() => RunAsync(EditPlayerAsync), HandleUnexpectedFailure, CanChangePlayer);
        TogglePlayerActiveCommand = new AsyncCommand(
            () => RunAsync(TogglePlayerActiveAsync),
            HandleUnexpectedFailure,
            () => CanChangePlayer() && SelectedPlayer is not { IsArchived: true });
        TogglePlayerArchivedCommand = new AsyncCommand(
            () => RunAsync(TogglePlayerArchivedAsync),
            HandleUnexpectedFailure,
            () => CanChangePlayer() && SelectedPlayer is { IsArchived: true } or { IsActive: false });
        SearchCharactersCommand = new AsyncCommand(() => RunAsync(SearchCharactersAsync), HandleUnexpectedFailure,
            () => !IsBusy && !string.IsNullOrWhiteSpace(CharacterSearch));
        ClearCharacterSearchCommand = new AsyncCommand(() => RunAsync(ClearCharacterSearchAsync), HandleUnexpectedFailure,
            () => !IsBusy && IsCharacterSearchActive);
        FindByQrCommand = new AsyncCommand(() => RunAsync(FindByQrAsync), HandleUnexpectedFailure,
            () => !IsBusy && !string.IsNullOrWhiteSpace(QrIdentifierSearch));
        PreviousCharactersCommand = new AsyncCommand(
            () => RunAsync(() => ShowCharacterResultsAsync(_characterResults?.FindLinkHref(SirenRelations.Previous))),
            HandleUnexpectedFailure,
            () => !IsBusy && _characterResults?.FindLinkHref(SirenRelations.Previous) is not null);
        NextCharactersCommand = new AsyncCommand(
            () => RunAsync(() => ShowCharacterResultsAsync(_characterResults?.FindLinkHref(SirenRelations.Next))),
            HandleUnexpectedFailure,
            () => !IsBusy && _characterResults?.FindLinkHref(SirenRelations.Next) is not null);
        NewCharacterCommand = new AsyncCommand(
            () => RunAsync(CreateCharacterAsync),
            HandleUnexpectedFailure,
            () => CanWrite && !IsBusy && SelectedPlayer is { IsActive: true });
        EditCharacterCommand = new AsyncCommand(() => RunAsync(EditCharacterAsync), HandleUnexpectedFailure, CanChangeCharacter);
        ToggleCharacterArchivedCommand = new AsyncCommand(
            () => RunAsync(ToggleCharacterArchivedAsync),
            HandleUnexpectedFailure,
            () => CanChangeCharacter() && SelectedCharacter is { IsArchived: true } or { IsActive: false });
        RegenerateQrCommand = new AsyncCommand(() => RunAsync(RegenerateQrAsync), HandleUnexpectedFailure, CanChangeCharacter);
        ManageStatsCommand = new AsyncCommand(() => RunAsync(ManageStatsAsync), HandleUnexpectedFailure,
            () => !IsBusy && _players is not null);
        PreviewCardsCommand = new AsyncCommand(() => RunAsync(PreviewCardsAsync), HandleUnexpectedFailure, CanPrintCards);
        ExportCardsCommand = new AsyncCommand(() => RunAsync(ExportCardsAsync), HandleUnexpectedFailure, CanPrintCards);
        SelectedCharacters.CollectionChanged += (_, _) => NotifyCommandsChanged();
    }

    public AsyncCommand SearchPlayersCommand { get; }

    public AsyncCommand PreviousPlayersCommand { get; }

    public AsyncCommand NextPlayersCommand { get; }

    public AsyncCommand NewPlayerCommand { get; }

    public AsyncCommand EditPlayerCommand { get; }

    public AsyncCommand TogglePlayerActiveCommand { get; }

    public AsyncCommand TogglePlayerArchivedCommand { get; }

    public AsyncCommand SearchCharactersCommand { get; }

    public AsyncCommand ClearCharacterSearchCommand { get; }

    public AsyncCommand FindByQrCommand { get; }

    public AsyncCommand PreviousCharactersCommand { get; }

    public AsyncCommand NextCharactersCommand { get; }

    public AsyncCommand NewCharacterCommand { get; }

    public AsyncCommand EditCharacterCommand { get; }

    public AsyncCommand ToggleCharacterArchivedCommand { get; }

    public AsyncCommand RegenerateQrCommand { get; }

    public AsyncCommand ManageStatsCommand { get; }

    public AsyncCommand PreviewCardsCommand { get; }

    public AsyncCommand ExportCardsCommand { get; }

    public ObservableCollection<PlayerListItem> Players { get; } = [];

    public ObservableCollection<CharacterListItem> Characters { get; } = [];

    /// <summary>The characters whose cards are printed, kept in step with the character list's selection.</summary>
    public ObservableCollection<CharacterListItem> SelectedCharacters { get; } = [];

    public PlayerListItem? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (_selectedPlayer == value)
            {
                return;
            }

            var changedPlayer = _selectedPlayer?.PlayerId != value?.PlayerId;
            _selectedPlayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TogglePlayerActiveLabel));
            OnPropertyChanged(nameof(TogglePlayerArchivedLabel));
            NotifyCommandsChanged();
            if (changedPlayer && !IsCharacterSearchActive)
            {
                _reloadCharactersCommand.Execute(null);
            }
        }
    }

    public CharacterListItem? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (_selectedCharacter == value)
            {
                return;
            }

            _selectedCharacter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToggleCharacterArchivedLabel));
            NotifyCommandsChanged();
        }
    }

    public string PlayerSearch
    {
        get => _playerSearch;
        set => SetText(ref _playerSearch, value);
    }

    public string CharacterSearch
    {
        get => _characterSearch;
        set
        {
            SetText(ref _characterSearch, value);
            SearchCharactersCommand.NotifyCanExecuteChanged();
        }
    }

    public string QrIdentifierSearch
    {
        get => _qrIdentifier;
        set
        {
            SetText(ref _qrIdentifier, value);
            FindByQrCommand.NotifyCanExecuteChanged();
        }
    }

    public bool ShowArchivedPlayers
    {
        get => _showArchivedPlayers;
        set
        {
            if (_showArchivedPlayers == value)
            {
                return;
            }

            _showArchivedPlayers = value;
            OnPropertyChanged();
            _reloadPlayersCommand.Execute(null);
        }
    }

    public bool ShowArchivedCharacters
    {
        get => _showArchivedCharacters;
        set
        {
            if (_showArchivedCharacters == value)
            {
                return;
            }

            _showArchivedCharacters = value;
            OnPropertyChanged();
            _reloadCharactersCommand.Execute(null);
        }
    }

    /// <summary>Whether the character list shows a search across every character instead of the selected player's.</summary>
    public bool IsCharacterSearchActive
    {
        get => _isCharacterSearchActive;
        private set
        {
            if (_isCharacterSearchActive == value)
            {
                return;
            }

            _isCharacterSearchActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CharactersHeading));
            ClearCharacterSearchCommand.NotifyCanExecuteChanged();
        }
    }

    public string PlayersHeading => Translate("players.players");

    public string CharactersHeading => Translate(IsCharacterSearchActive ? "players.characterResults" : "players.characters");

    public string PlayerSearchLabel => Translate("players.searchPlayers");

    public string CharacterSearchLabel => Translate("players.searchCharacters");

    public string QrIdentifierLabel => Translate("characters.qrIdentifier");

    public string SearchLabel => Translate("players.search");

    public string ClearSearchLabel => Translate("players.clearSearch");

    public string FindByQrLabel => Translate("players.findByQr");

    public string ShowArchivedLabel => Translate("skills.showArchived");

    public string PreviousLabel => Translate("players.previousPage");

    public string NextLabel => Translate("players.nextPage");

    public string NewLabel => Translate("skills.new");

    public string EditLabel => Translate("skills.edit");

    public string StatsLabel => Translate("players.manageStats");

    public string RegenerateQrLabel => Translate("players.regenerateQr");

    public string PreviewCardsLabel => Translate("players.previewCards");

    public string ExportCardsLabel => Translate("players.exportCards");

    public string TogglePlayerActiveLabel =>
        Translate(SelectedPlayer is { IsActive: false } ? "players.activate" : "players.deactivate");

    public string TogglePlayerArchivedLabel =>
        Translate(SelectedPlayer is { IsArchived: true } ? "skills.unarchive" : "skills.archive");

    public string ToggleCharacterArchivedLabel =>
        Translate(SelectedCharacter is { IsArchived: true } ? "skills.unarchive" : "skills.archive");

    public string PlayerPage { get; private set; } = string.Empty;

    public string CharacterPage { get; private set; } = string.Empty;

    public string? PlayersPlaceholder => Players.Count == 0 ? Translate("common.noResults") : null;

    public bool HasPlayersPlaceholder => PlayersPlaceholder is not null;

    public string? CharactersPlaceholder =>
        Characters.Count > 0 ? null
        : IsCharacterSearchActive || SelectedPlayer is not null ? Translate("common.noResults")
        : Translate("players.selectPlayer");

    public bool HasCharactersPlaceholder => CharactersPlaceholder is not null;

    public override Task LoadAsync() => RunAsync(() => LoadPlayersCoreAsync(null));

    private protected override void OnLocaleChanged()
    {
        foreach (var name in new[]
                 {
                     nameof(PlayersHeading), nameof(CharactersHeading), nameof(PlayerSearchLabel), nameof(CharacterSearchLabel),
                     nameof(QrIdentifierLabel), nameof(SearchLabel), nameof(ClearSearchLabel), nameof(FindByQrLabel),
                     nameof(ShowArchivedLabel), nameof(PreviousLabel), nameof(NextLabel), nameof(NewLabel), nameof(EditLabel),
                     nameof(StatsLabel), nameof(RegenerateQrLabel), nameof(PreviewCardsLabel), nameof(ExportCardsLabel),
                     nameof(TogglePlayerActiveLabel),
                     nameof(TogglePlayerArchivedLabel), nameof(ToggleCharacterArchivedLabel)
                 })
        {
            OnPropertyChanged(name);
        }

        _reloadPlayersCommand.Execute(null);
    }

    private protected override void NotifyCommandsChanged()
    {
        foreach (var command in new[]
                 {
                     SearchPlayersCommand, PreviousPlayersCommand, NextPlayersCommand, NewPlayerCommand, EditPlayerCommand,
                     TogglePlayerActiveCommand, TogglePlayerArchivedCommand, SearchCharactersCommand,
                     ClearCharacterSearchCommand, FindByQrCommand, PreviousCharactersCommand, NextCharactersCommand,
                     NewCharacterCommand, EditCharacterCommand, ToggleCharacterArchivedCommand, RegenerateQrCommand,
                     ManageStatsCommand, PreviewCardsCommand, ExportCardsCommand
                 })
        {
            command.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Loads <paramref name="href"/>, or runs the player search with the current search text and filter.</summary>
    private async Task LoadPlayersCoreAsync(string? href)
    {
        var generation = ++_playerLoadGeneration;
        var collection = await GetAsync(href ?? _playersHref);
        if (href is null && (!string.IsNullOrWhiteSpace(PlayerSearch) || ShowArchivedPlayers))
        {
            var search = collection.FindAction("search-players") ?? throw MissingAction();
            collection = await GetAsync(search.ToQueryHref(new Dictionary<string, string?>
            {
                ["search"] = PlayerSearch,
                ["includeArchived"] = ShowArchivedPlayers ? "true" : null
            }));
        }

        if (generation != _playerLoadGeneration)
        {
            return;
        }

        _players = collection;
        IsLoaded = true;
        CanWrite = collection.FindAction("create-player") is not null;
        var page = SirenProperties.Read<RosterCollectionProperties>(collection.Properties);
        PlayerPage = PageText(page);
        OnPropertyChanged(nameof(PlayerPage));
        var selectedId = SelectedPlayer?.PlayerId;
        ReplaceAll(Players, (collection.Entities ?? []).Select(ToPlayerItem));
        SelectedPlayer = Players.FirstOrDefault(player => player.PlayerId == selectedId);
        OnPropertyChanged(nameof(PlayersPlaceholder));
        OnPropertyChanged(nameof(HasPlayersPlaceholder));
        NotifyCommandsChanged();
        await LoadCharactersCoreAsync();
    }

    /// <summary>Reloads the character list: the active search results, or else the selected player's characters.</summary>
    private async Task LoadCharactersCoreAsync()
    {
        if (IsCharacterSearchActive)
        {
            if (_characterResults?.FindLinkHref("self") is { } self)
            {
                await ShowCharacterResultsAsync(self);
            }

            return;
        }

        var generation = ++_characterLoadGeneration;
        SirenEntity? player = null;
        if (SelectedPlayer is { } selected)
        {
            player = await GetAsync(selected.Href);
            if (ShowArchivedCharacters)
            {
                player = await GetAsync(player.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
            }
        }

        if (generation != _characterLoadGeneration)
        {
            return;
        }

        _selectedPlayerResource = player;
        ShowCharacters(player?.Entities ?? [], showPlayer: false);
        CharacterPage = string.Empty;
        OnPropertyChanged(nameof(CharacterPage));
    }

    private async Task SearchCharactersAsync()
    {
        var search = (_players ?? throw MissingAction()).FindAction("search-characters") ?? throw MissingAction();
        IsCharacterSearchActive = true;
        await ShowCharacterResultsAsync(search.ToQueryHref(new Dictionary<string, string?>
        {
            ["search"] = CharacterSearch,
            ["includeArchived"] = ShowArchivedCharacters ? "true" : null
        }));
    }

    private async Task ShowCharacterResultsAsync(string? href)
    {
        var generation = ++_characterLoadGeneration;
        var results = await GetAsync(href ?? throw MissingAction());
        if (generation != _characterLoadGeneration)
        {
            return;
        }

        _characterResults = results;
        ShowCharacters(results.Entities ?? [], showPlayer: true);
        CharacterPage = PageText(SirenProperties.Read<RosterCollectionProperties>(results.Properties));
        OnPropertyChanged(nameof(CharacterPage));
    }

    private async Task ClearCharacterSearchAsync()
    {
        IsCharacterSearchActive = false;
        _characterResults = null;
        CharacterSearch = string.Empty;
        QrIdentifierSearch = string.Empty;
        await LoadCharactersCoreAsync();
    }

    private async Task FindByQrAsync()
    {
        var find = (_players ?? throw MissingAction()).FindAction("find-character-by-qr") ?? throw MissingAction();
        var character = await GetAsync(find.ToQueryHref(new Dictionary<string, string?> { ["identifier"] = QrIdentifierSearch }));
        ++_characterLoadGeneration;
        IsCharacterSearchActive = true;
        _characterResults = null;
        ShowCharacters(
            [new SirenSubEntity { Rel = ["item"], Href = character.FindLinkHref("self"), Properties = character.Properties }],
            showPlayer: true);
        CharacterPage = string.Empty;
        OnPropertyChanged(nameof(CharacterPage));
    }

    private void ShowCharacters(IEnumerable<SirenSubEntity> entities, bool showPlayer)
    {
        var selectedId = SelectedCharacter?.CharacterId;
        ReplaceAll(Characters, entities.Select(entity => ToCharacterItem(entity, showPlayer)));
        foreach (var stale in SelectedCharacters.Except(Characters).ToArray())
        {
            SelectedCharacters.Remove(stale);
        }

        SelectedCharacter = Characters.FirstOrDefault(character => character.CharacterId == selectedId);
        OnPropertyChanged(nameof(CharactersPlaceholder));
        OnPropertyChanged(nameof(HasCharactersPlaceholder));
        NotifyCommandsChanged();
    }

    private async Task CreatePlayerAsync()
    {
        var action = (_players ?? throw MissingAction()).FindAction("create-player") ?? throw MissingAction();
        SirenEntity? saved = null;
        var editor = new PlayerEditorViewModel(
            Localizer,
            Locale,
            "players.newPlayer",
            Choices(page => page.PlayerTypes),
            editor => SubmitEditorAsync(
                editor,
                async () => saved = saved is null
                    ? await SubmitAsync(action, editor.CreateBody(null))
                    : await UpdatePlayerAsync(saved, editor),
                null));
        await _dialogs.ShowPlayerEditorAsync(editor);
        await ReloadPlayersAfterSaveAsync(editor.IsSaved);
    }

    private async Task EditPlayerAsync()
    {
        var saved = await GetAsync(SelectedPlayer?.Href ?? throw MissingAction());
        var editor = new PlayerEditorViewModel(
            Localizer,
            Locale,
            "players.editPlayer",
            Choices(page => page.PlayerTypes),
            editor => SubmitEditorAsync(
                editor,
                async () => saved = await UpdatePlayerAsync(saved, editor),
                async () =>
                {
                    saved = await GetAsync(saved.FindLinkHref("self") ?? throw MissingAction());
                    editor.Apply(SirenProperties.Read<PlayerProperties>(saved.Properties));
                }));
        editor.Apply(SirenProperties.Read<PlayerProperties>(saved.Properties));
        await _dialogs.ShowPlayerEditorAsync(editor);
        await ReloadPlayersAfterSaveAsync(editor.IsSaved);
    }

    private Task<SirenEntity> UpdatePlayerAsync(SirenEntity saved, PlayerEditorViewModel editor) =>
        SubmitAsync(
            saved.FindAction("update-player") ?? throw MissingAction(),
            editor.CreateBody(SirenProperties.Read<PlayerProperties>(saved.Properties).Version));

    private async Task ReloadPlayersAfterSaveAsync(bool isSaved)
    {
        if (!isSaved)
        {
            return;
        }

        await LoadPlayersCoreAsync(_players?.FindLinkHref("self"));
        StatusMessage = Translate("actions.completed");
    }

    private Task TogglePlayerActiveAsync()
    {
        var player = SelectedPlayer ?? throw MissingAction();
        return player.IsActive
            ? ConfirmAndSubmitAsync(player.Href, "deactivate-player", "players.confirmDeactivate", "players.deactivate", player.Name)
            : ConfirmAndSubmitAsync(player.Href, "activate-player", "players.confirmActivate", "players.activate", player.Name);
    }

    private Task TogglePlayerArchivedAsync()
    {
        var player = SelectedPlayer ?? throw MissingAction();
        return player.IsArchived
            ? ConfirmAndSubmitAsync(player.Href, "unarchive-player", "players.confirmUnarchive", "skills.unarchive", player.Name)
            : ConfirmAndSubmitAsync(player.Href, "archive-player", "players.confirmArchive", "skills.archive", player.Name);
    }

    private Task ToggleCharacterArchivedAsync()
    {
        var character = SelectedCharacter ?? throw MissingAction();
        return character.IsArchived
            ? ConfirmAndSubmitAsync(character.Href, "unarchive-character", "characters.confirmUnarchive", "skills.unarchive", character.Name)
            : ConfirmAndSubmitAsync(character.Href, "archive-character", "characters.confirmArchive", "skills.archive", character.Name);
    }

    private Task RegenerateQrAsync()
    {
        var character = SelectedCharacter ?? throw MissingAction();
        return ConfirmAndSubmitAsync(
            character.Href, "regenerate-qr-identifier", "characters.confirmRegenerateQr", "players.regenerateQr", character.Name);
    }

    private async Task ConfirmAndSubmitAsync(string href, string actionName, string messageKey, string confirmKey, string name)
    {
        var resource = await GetAsync(href);
        var action = resource.FindAction(actionName) ?? throw MissingAction();
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            string.Format(CultureInfo.CurrentCulture, Translate(messageKey), name),
            Translate(confirmKey),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        await SubmitAsync(action, null);
        await LoadPlayersCoreAsync(_players?.FindLinkHref("self"));
        StatusMessage = Translate("actions.completed");
    }

    private async Task CreateCharacterAsync()
    {
        var player = _selectedPlayerResource ?? throw MissingAction();
        var action = player.FindAction("create-character") ?? throw MissingAction();
        SirenEntity? saved = null;
        var editor = NewCharacterEditor("characters.newCharacter", isExisting: false, editor => SubmitEditorAsync(
            editor,
            async () => saved = saved is null
                ? await SubmitAsync(action, editor.CreateBody(null))
                : await UpdateCharacterAsync(saved, editor),
            null));
        var stats = await GetAsync(player.FindLinkHref(SirenRelations.CharacterStats) ?? throw MissingAction());
        stats = await GetAsync(stats.FindLinkHref(SirenRelations.WithArchived) ?? throw MissingAction());
        editor.SetStats((stats.Entities ?? []).Select(entity =>
        {
            var stat = SirenProperties.Read<CharacterStatProperties>(entity.Properties);
            return new CharacterStatValueProperties(stat.StatId, stat.Name, stat.Type, stat.DefaultNumber, stat.DefaultText, stat.IsArchived);
        }));
        await ApplyNetworksAsync(editor, player, null);
        await _dialogs.ShowCharacterEditorAsync(editor);
        await ReloadCharactersAfterSaveAsync(editor.IsSaved);
    }

    private async Task EditCharacterAsync()
    {
        var saved = await GetAsync(SelectedCharacter?.Href ?? throw MissingAction());
        var editor = NewCharacterEditor("characters.editCharacter", isExisting: true, editor => SubmitEditorAsync(
            editor,
            async () => saved = await UpdateCharacterAsync(saved, editor),
            async () =>
            {
                saved = await GetAsync(saved.FindLinkHref("self") ?? throw MissingAction());
                editor.Apply(SirenProperties.Read<CharacterProperties>(saved.Properties));
            }));
        editor.EnableManageSkills(async () =>
        {
            if (await ShowCharacterSkillsAsync(saved))
            {
                // Saving talents and skills raises the character version; the dialog's other fields are unchanged.
                saved = await GetAsync(saved.FindLinkHref("self") ?? throw MissingAction());
            }
        });
        var character = SirenProperties.Read<CharacterProperties>(saved.Properties);
        editor.Apply(character);
        await ApplyNetworksAsync(editor, saved, character.NetworkId);
        await _dialogs.ShowCharacterEditorAsync(editor);
        await ReloadCharactersAfterSaveAsync(editor.IsSaved);
    }

    /// <summary>Shows the character's talents and skills window; returns whether anything was saved there.</summary>
    private async Task<bool> ShowCharacterSkillsAsync(SirenEntity character)
    {
        var skills = new CharacterSkillsViewModel(
            ApiClient,
            Localizer,
            ApiBaseUrl,
            Locale,
            character.FindLinkHref(SirenRelations.CharacterSkills) ?? throw MissingAction());
        skills.SessionExpired += (_, _) =>
            StatusMessage = Report(new ManagementApiException("errors.unauthorized", System.Net.HttpStatusCode.Unauthorized));
        await skills.LoadAsync();
        await _dialogs.ShowCharacterSkillsAsync(skills);
        return skills.IsSaved;
    }

    /// <summary>
    /// Creates the character dialog. A save with an invalid number field shows the error in the dialog and sends
    /// nothing.
    /// </summary>
    private CharacterEditorViewModel NewCharacterEditor(
        string titleKey,
        bool isExisting,
        Func<CharacterEditorViewModel, Task<bool>> submit) =>
        new(
            Localizer,
            Locale,
            titleKey,
            Choices(page => page.CharacterStatuses),
            Choices(page => page.StatTypes),
            isExisting,
            editor => editor.ValidateNumbers() ? submit(editor) : Task.FromResult(false));

    private Task<SirenEntity> UpdateCharacterAsync(SirenEntity saved, CharacterEditorViewModel editor) =>
        SubmitAsync(
            saved.FindAction("update-character") ?? throw MissingAction(),
            editor.CreateBody(SirenProperties.Read<CharacterProperties>(saved.Properties).Version));

    /// <summary>
    /// Lists the networks a character can pick: none, or any enabled network other than the default. The character's
    /// current network is kept in the list when it was disabled or became the default.
    /// </summary>
    private async Task ApplyNetworksAsync(CharacterEditorViewModel editor, SirenEntity resource, Guid? currentNetworkId)
    {
        var networks = await GetAsync(resource.FindLinkHref(SirenRelations.Networks) ?? throw MissingAction());
        networks = await GetAsync(networks.FindLinkHref(SirenRelations.WithDisabled) ?? throw MissingAction());
        var all = (networks.Entities ?? []).Select(entity => SirenProperties.Read<NetworkProperties>(entity.Properties)).ToArray();
        var defaultNetwork = all.FirstOrDefault(network => network.IsDefault);
        var choices = new List<NetworkChoice> { new(null, Translate("characters.noNetwork")) };
        choices.AddRange(all
            .Where(network => network.NetworkId == currentNetworkId || (network.IsEnabled && !network.IsDefault))
            .Select(network => new NetworkChoice(network.NetworkId, NetworkTitle(network))));
        editor.SetNetworks(
            defaultNetwork is null
                ? Translate("characters.noDefaultNetwork")
                : string.Format(CultureInfo.CurrentCulture, Translate("characters.defaultNetwork"), defaultNetwork.Name),
            choices,
            currentNetworkId);
    }

    private string NetworkTitle(NetworkProperties network) =>
        network.IsDefault ? $"{network.Name} {Translate("networks.defaultTag")}"
        : network.IsEnabled ? network.Name
        : $"{network.Name} {Translate("networks.disabledTag")}";

    private async Task ReloadCharactersAfterSaveAsync(bool isSaved)
    {
        if (!isSaved)
        {
            return;
        }

        await LoadCharactersCoreAsync();
        StatusMessage = Translate("actions.completed");
    }

    private async Task ManageStatsAsync()
    {
        var players = _players ?? throw MissingAction();
        var typeTitles = SirenProperties.Read<RosterCollectionProperties>(players.Properties).StatTypes
            .ToDictionary(type => type.Name, type => type.Title);
        var stats = new CharacterStatsViewModel(
            ApiClient,
            Localizer,
            _dialogs,
            ApiBaseUrl,
            Locale,
            players.FindLinkHref(SirenRelations.CharacterStats) ?? throw MissingAction(),
            typeTitles,
            CanWrite);
        stats.SessionExpired += (_, _) =>
            StatusMessage = Report(new ManagementApiException("errors.unauthorized", System.Net.HttpStatusCode.Unauthorized));
        await stats.LoadAsync();
        await _dialogs.ShowStatsAsync(stats);
        await LoadCharactersCoreAsync();
    }

    private async Task PreviewCardsAsync()
    {
        if (!await _dialogs.PreviewPdfAsync(await PrintCardsAsync(), CardsFileName))
        {
            StatusMessage = Translate("players.cardsPreviewFailed");
        }
    }

    private async Task ExportCardsAsync()
    {
        if (await _dialogs.ExportPdfAsync(await PrintCardsAsync(), CardsFileName, Translate("players.cardsFileType")))
        {
            StatusMessage = Translate("players.cardsExported");
        }
    }

    /// <summary>Prints the selected characters' cards, in the order the list shows them.</summary>
    private Task<byte[]> PrintCardsAsync()
    {
        var action = (_players ?? throw MissingAction()).FindAction(PrintCardsAction) ?? throw MissingAction();
        var characterIds = Characters.Where(SelectedCharacters.Contains).Select(character => character.CharacterId).ToArray();
        return ApiClient.DownloadActionAsync(
            ApiBaseUrl,
            action,
            JsonSerializer.SerializeToElement(new { characterIds }, JsonOptions),
            PdfMediaType,
            Locale);
    }

    private bool CanPrintCards() =>
        !IsBusy && SelectedCharacters.Count > 0 && _players?.FindAction(PrintCardsAction) is not null;

    private RosterChoice[] Choices(Func<RosterCollectionProperties, IReadOnlyList<RosterOptionProperties>> select) =>
        select(SirenProperties.Read<RosterCollectionProperties>((_players ?? throw MissingAction()).Properties))
            .Select(option => new RosterChoice(option.Name, option.Title))
            .ToArray();

    private string PageText(RosterCollectionProperties page) =>
        string.Format(CultureInfo.CurrentCulture, Translate("players.page"), page.PageNumber, page.PageCount, page.TotalCount);

    private PlayerListItem ToPlayerItem(SirenSubEntity entity)
    {
        var player = SirenProperties.Read<PlayerProperties>(entity.Properties);
        var isActive = player.Status == ActiveStatus;
        var tags = new List<string>();
        if (!isActive)
        {
            tags.Add(Translate("players.inactiveTag"));
        }

        if (player.IsArchived)
        {
            tags.Add(Translate("skills.archivedTag"));
        }

        return new PlayerListItem(
            player.PlayerId,
            player.DisplayName,
            isActive,
            player.IsArchived,
            entity.Href ?? throw MissingAction(),
            string.Join(' ', [$"#{player.Number} {player.DisplayName}", .. tags]));
    }

    private CharacterListItem ToCharacterItem(SirenSubEntity entity, bool showPlayer)
    {
        var character = SirenProperties.Read<CharacterProperties>(entity.Properties);
        var statusTitles = _players is null
            ? new Dictionary<string, string>()
            : SirenProperties.Read<RosterCollectionProperties>(_players.Properties).CharacterStatuses
                .ToDictionary(status => status.Name, status => status.Title);
        var name = showPlayer ? $"#{character.Number} {character.Name} — {character.PlayerDisplayName}" : $"#{character.Number} {character.Name}";
        var tags = new List<string> { $"({statusTitles.GetValueOrDefault(character.Status, character.Status)})" };
        if (character.IsArchived)
        {
            tags.Add(Translate("skills.archivedTag"));
        }

        return new CharacterListItem(
            character.CharacterId,
            character.PlayerId,
            character.Name,
            character.Status == ActiveStatus,
            character.IsArchived,
            entity.Href ?? throw MissingAction(),
            string.Join(' ', [name, .. tags]));
    }

    private bool CanChangePlayer() => CanWrite && !IsBusy && SelectedPlayer is not null;

    private bool CanChangeCharacter() => CanWrite && !IsBusy && SelectedCharacter is not null;

    private void SetText(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}
