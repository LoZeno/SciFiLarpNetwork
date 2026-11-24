using System.Collections.ObjectModel;
using System.Globalization;
using SciFiLarpNetwork.Common.NewsMarkdown;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.News;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>
/// The media library dialog: every picture and video, a preview of the selected one, and, for administrators, upload
/// and removal. The upload limits are always shown. When a story is being written, the dialog also lets the user
/// choose the media to place, with its text and, for a video, its play settings.
/// </summary>
public sealed class NewsMediaLibraryViewModel : ScreenViewModel
{
    private static readonly Dictionary<string, string> FormatNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = "PNG",
        ["image/jpeg"] = "JPEG",
        ["image/webp"] = "WebP",
        ["image/gif"] = "GIF",
        ["video/webm"] = "WebM",
        ["video/mp4"] = "MP4"
    };

    private readonly INewsDialogService _dialogs;
    private readonly NewsMediaCache _cache;
    private readonly string _mediaHref;
    private readonly AsyncCommand _showSelectedCommand;
    private SirenEntity? _library;
    private NewsMediaCollectionProperties? _limits;
    private NewsMediaListItem? _selectedItem;
    private string _uploadName = string.Empty;
    private string _mediaText = string.Empty;
    private bool _autoplay;
    private bool _loop;
    private bool _controls = true;

    internal NewsMediaLibraryViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        INewsDialogService dialogs,
        NewsMediaCache cache,
        string apiBaseUrl,
        string locale,
        string mediaHref,
        bool isChoosing)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _cache = cache;
        _mediaHref = mediaHref;
        IsChoosing = isChoosing;
        _showSelectedCommand = new AsyncCommand(ShowSelectedAsync, HandleUnexpectedFailure);
        UploadCommand = new AsyncCommand(() => RunAsync(UploadAsync), HandleUnexpectedFailure,
            () => !IsBusy && _library?.FindAction("upload-news-media") is not null);
        RemoveCommand = new AsyncCommand(() => RunAsync(RemoveAsync), HandleUnexpectedFailure,
            () => !IsBusy && SelectedItem?.Media.RemoveAction is not null);
        ChooseCommand = new DelegateCommand(Choose, () => IsChoosing && SelectedItem is not null);
        CloseCommand = new DelegateCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? CloseRequested;

    /// <summary>Whether the dialog was opened to choose media for a story.</summary>
    public bool IsChoosing { get; }

    /// <summary>The media chosen for the story; <see langword="null"/> until the user chooses.</summary>
    public NewsMediaChoice? Chosen { get; private set; }

    public AsyncCommand UploadCommand { get; }

    public AsyncCommand RemoveCommand { get; }

    public DelegateCommand ChooseCommand { get; }

    public DelegateCommand CloseCommand { get; }

    public ObservableCollection<NewsMediaListItem> Items { get; } = [];

    public NewsMediaListItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value)
            {
                return;
            }

            _selectedItem = value;
            MediaText = value?.Media.Name ?? string.Empty;
            OnPropertyChanged();
            RefreshSelection();
            NotifyCommandsChanged();
            _showSelectedCommand.Execute(null);
        }
    }

    public string UploadName
    {
        get => _uploadName;
        set
        {
            if (_uploadName == value)
            {
                return;
            }

            _uploadName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The text shown to people who cannot see the chosen media; it starts as the media's name.</summary>
    public string MediaText
    {
        get => _mediaText;
        set
        {
            if (_mediaText == value)
            {
                return;
            }

            _mediaText = value;
            OnPropertyChanged();
        }
    }

    public bool Autoplay
    {
        get => _autoplay;
        set => SetOption(ref _autoplay, value);
    }

    public bool Loop
    {
        get => _loop;
        set => SetOption(ref _loop, value);
    }

    public bool Controls
    {
        get => _controls;
        set => SetOption(ref _controls, value);
    }

    public bool HasSelection => SelectedItem is not null;

    public bool IsSelectedVideo => SelectedItem?.Media.IsVideo == true;

    public bool IsSelectedPicture => SelectedItem?.Media.IsPicture == true;

    /// <summary>The downloaded picture shown in the preview; <see langword="null"/> for a video or while loading.</summary>
    public byte[]? SelectedPicture => SelectedItem is { Media.IsPicture: true } item
        ? _cache.Find(item.Media.MediaId)?.PictureContent
        : null;

    public string SelectedDescription => SelectedItem?.DisplayName ?? string.Empty;

    public string LimitsText => _limits is { } limits
        ? string.Format(
            CultureInfo.CurrentCulture,
            Translate("news.mediaLimits"),
            FormatList(limits.PictureMediaTypes),
            NewsSizes.Format(limits.MaximumPictureByteCount),
            limits.MaximumPixelDimension,
            FormatList(limits.VideoMediaTypes),
            NewsSizes.Format(limits.MaximumVideoByteCount))
        : string.Empty;

    public int MaximumNameLength => _limits?.MaximumNameLength ?? 128;

    public string Title => Translate("news.mediaLibrary");

    public string UploadNameLabel => Translate("news.mediaName");

    public string UploadLabel => Translate("news.uploadMedia");

    public string RemoveLabel => Translate("news.removeMedia");

    public string ChooseLabel => Translate("news.insertMedia");

    public string CloseLabel => Translate("common.close");

    public string MediaTextLabel => Translate("news.mediaText");

    public string AutoplayLabel => Translate("news.autoplay");

    public string LoopLabel => Translate("news.loop");

    public string ControlsLabel => Translate("news.controls");

    public string AutoplayNote => Translate("news.autoplayMuted");

    public string VideoNote => Translate("news.videoNotPlayed");

    public string? Placeholder => _library is not null && Items.Count == 0 ? Translate("news.noMedia") : null;

    public bool HasPlaceholder => Placeholder is not null;

    public override Task LoadAsync() => RunAsync(() => LoadLibraryAsync());

    private protected override void OnLocaleChanged()
    {
        foreach (var name in new[]
                 {
                     nameof(Title), nameof(UploadNameLabel), nameof(UploadLabel), nameof(RemoveLabel), nameof(ChooseLabel),
                     nameof(CloseLabel), nameof(MediaTextLabel), nameof(AutoplayLabel), nameof(LoopLabel),
                     nameof(ControlsLabel), nameof(AutoplayNote), nameof(VideoNote), nameof(LimitsText), nameof(Placeholder)
                 })
        {
            OnPropertyChanged(name);
        }
    }

    private protected override void NotifyCommandsChanged()
    {
        UploadCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        ChooseCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadLibraryAsync(Guid? select = null)
    {
        _library = await GetAsync(_mediaHref);
        _limits = SirenProperties.Read<NewsMediaCollectionProperties>(_library.Properties);
        IsLoaded = true;
        CanWrite = _library.FindAction("upload-news-media") is not null;
        var media = (_library.Entities ?? []).Select(NewsMediaMappings.ToMediaItem).ToArray();
        _cache.Remember(media);
        var selectedId = select ?? SelectedItem?.Media.MediaId;
        ReplaceAll(Items, media.Select(item => new NewsMediaListItem(item, Describe(item))));
        SelectedItem = Items.FirstOrDefault(item => item.Media.MediaId == selectedId);
        foreach (var name in new[] { nameof(LimitsText), nameof(MaximumNameLength), nameof(Placeholder), nameof(HasPlaceholder) })
        {
            OnPropertyChanged(name);
        }

        NotifyCommandsChanged();
    }

    private async Task ShowSelectedAsync()
    {
        if (SelectedItem is { Media.IsPicture: true } item && await _cache.LoadPicturesAsync([item.Media.MediaId], Locale))
        {
            OnPropertyChanged(nameof(SelectedPicture));
        }
    }

    /// <summary>
    /// Asks for a file and uploads it under the typed name, or the file's own name without its extension. A file
    /// larger than the largest video is refused before it is sent.
    /// </summary>
    private async Task UploadAsync()
    {
        var action = _library?.FindAction("upload-news-media") ?? throw MissingAction();
        var limits = _limits ?? throw MissingAction();
        if (await _dialogs.PickMediaFileAsync(limits.MaximumVideoByteCount) is not { } file)
        {
            return;
        }

        if (file.Content.LongLength > limits.MaximumVideoByteCount)
        {
            StatusMessage = Translate("errors.mediaTooLarge");
            return;
        }

        var name = string.IsNullOrWhiteSpace(UploadName) ? Path.GetFileNameWithoutExtension(file.FileName) : UploadName;
        var uploaded = await ApiClient.UploadFileActionAsync(
            ApiBaseUrl,
            action,
            new Dictionary<string, string> { ["name"] = name },
            new UploadFile("file", file.FileName, file.Content),
            Locale);
        UploadName = string.Empty;
        await LoadLibraryAsync(NewsMediaMappings.ToMediaItem(uploaded).MediaId);
        StatusMessage = Translate("actions.completed");
    }

    private async Task RemoveAsync()
    {
        if (SelectedItem is not { Media.RemoveAction: { } action } item)
        {
            throw MissingAction();
        }

        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("news.removeMedia"),
            string.Format(CultureInfo.CurrentCulture, Translate("news.removeMediaConfirm"), item.Media.Name),
            Translate("news.removeMedia"),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        await ApiClient.ExecuteActionAsync(ApiBaseUrl, action, Locale);
        SelectedItem = null;
        await LoadLibraryAsync();
        StatusMessage = Translate("actions.completed");
    }

    private void Choose()
    {
        if (SelectedItem is not { } item)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(MediaText))
        {
            StatusMessage = Translate("errors.missingMediaText");
            return;
        }

        var options = item.Media.IsVideo
            ? (Autoplay ? NewsVideoOptions.Autoplay : NewsVideoOptions.None) |
              (Loop ? NewsVideoOptions.Loop : NewsVideoOptions.None) |
              (Controls ? NewsVideoOptions.Controls : NewsVideoOptions.None)
            : NewsVideoOptions.None;
        Chosen = new NewsMediaChoice(_cache.Find(item.Media.MediaId) ?? item.Media,
            new NewsMediaEmbed(item.Media.MediaId, MediaText.Trim(), options));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetOption(ref bool option, bool value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (option == value)
        {
            return;
        }

        option = value;
        OnPropertyChanged(name);
    }

    private void RefreshSelection()
    {
        foreach (var name in new[]
                 {
                     nameof(HasSelection), nameof(IsSelectedVideo), nameof(IsSelectedPicture), nameof(SelectedPicture),
                     nameof(SelectedDescription)
                 })
        {
            OnPropertyChanged(name);
        }
    }

    private string Describe(NewsMediaItem media)
    {
        var kind = Translate(media.IsVideo ? "news.video" : "news.picture");
        var size = media is { Width: { } width, Height: { } height }
            ? $"{NewsSizes.Format(media.ByteCount)} · {width} × {height}"
            : NewsSizes.Format(media.ByteCount);
        return $"{media.Name} · {kind} · {FormatName(media.MediaType)} · {size}";
    }

    private static string FormatList(IEnumerable<string> mediaTypes) => string.Join(", ", mediaTypes.Select(FormatName));

    private static string FormatName(string mediaType) => FormatNames.GetValueOrDefault(mediaType, mediaType);
}
