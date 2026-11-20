using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Common.Siren;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Api.CardTemplates;
using SciFiLarpNetwork.Management.Api.Skills;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.CardTemplates;

/// <summary>
/// The card templates screen: whether the character-card template is the default or an uploaded one, and the
/// commands to download it and, for administrators, to preview a file, upload it, or reset to the default.
/// </summary>
public sealed class CardTemplateScreenViewModel : ScreenViewModel
{
    /// <summary>The largest template file accepted, in bytes (256 KB), the same limit the API enforces.</summary>
    public const int MaximumTemplateByteCount = 256 * 1024;

    private const string TemplateMediaType = "text/plain";
    private const string PdfMediaType = "application/pdf";
    private const string TemplateFileName = "character-cards.typ";
    private const string PreviewFileName = "character-cards-preview.pdf";
    private const string TemplateErrorKey = "template";
    private const string StaleVersionCode = "errors.staleVersion";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ICardTemplateDialogService _dialogs;
    private readonly string _templatesHref;
    private readonly AsyncCommand _reloadCommand;
    private SirenEntity? _template;
    private CardTemplateProperties? _properties;
    private string? _templateError;

    public CardTemplateScreenViewModel(
        IManagementApiClient apiClient,
        IManagementLocalizer localizer,
        ICardTemplateDialogService dialogs,
        string apiBaseUrl,
        string locale,
        string templatesHref)
        : base(apiClient, localizer, apiBaseUrl, locale)
    {
        _dialogs = dialogs;
        _templatesHref = templatesHref;
        _reloadCommand = new AsyncCommand(LoadAsync, HandleUnexpectedFailure);
        DownloadCommand = new AsyncCommand(() => RunAsync(DownloadAsync), HandleUnexpectedFailure,
            () => !IsBusy && _template?.FindLinkHref(SirenRelations.TemplateSource) is not null);
        PreviewFileCommand = new AsyncCommand(() => RunAsync(PreviewFileAsync), HandleUnexpectedFailure,
            () => CanRun("preview-card-template"));
        UploadCommand = new AsyncCommand(() => RunAsync(UploadAsync), HandleUnexpectedFailure,
            () => CanRun("upload-card-template"));
        ResetCommand = new AsyncCommand(() => RunAsync(ResetAsync), HandleUnexpectedFailure,
            () => CanRun("reset-card-template"));
    }

    public AsyncCommand DownloadCommand { get; }

    public AsyncCommand PreviewFileCommand { get; }

    public AsyncCommand UploadCommand { get; }

    public AsyncCommand ResetCommand { get; }

    /// <summary>Typst's error for the last file that was previewed or uploaded, if it was rejected.</summary>
    public string? TemplateError
    {
        get => _templateError;
        private set
        {
            if (_templateError == value)
            {
                return;
            }

            _templateError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasTemplateError));
        }
    }

    public bool HasTemplateError => !string.IsNullOrEmpty(TemplateError);

    public string Heading => Translate("api.cardTemplates");

    public string CharacterTemplateTitle => _properties?.Title ?? Translate("cardTemplates.character");

    /// <summary>"Default", or who uploaded the template in use and when, in the screen's language.</summary>
    public string CharacterTemplateState =>
        _properties switch
        {
            null => string.Empty,
            { IsDefault: true } => Translate("cardTemplates.default"),
            { } uploaded => string.Format(Culture, Translate("cardTemplates.uploaded"), uploaded.UploadedBy,
                uploaded.UploadedAtUtc?.ToLocalTime().ToString("g", Culture))
        };

    public string DownloadLabel => Translate("cardTemplates.download");

    public string PreviewFileLabel => Translate("cardTemplates.previewFile");

    public string UploadLabel => Translate("cardTemplates.upload");

    public string ResetLabel => Translate("cardTemplates.reset");

    public string TemplateErrorLabel => Translate("cardTemplates.typstError");

    private CultureInfo Culture => CultureInfo.GetCultureInfo(Locale);

    public override Task LoadAsync() => RunAsync(LoadTemplateAsync);

    private protected override void OnLocaleChanged()
    {
        OnPropertyChanged(nameof(Heading));
        OnPropertyChanged(nameof(DownloadLabel));
        OnPropertyChanged(nameof(PreviewFileLabel));
        OnPropertyChanged(nameof(UploadLabel));
        OnPropertyChanged(nameof(ResetLabel));
        OnPropertyChanged(nameof(TemplateErrorLabel));
        _reloadCommand.Execute(null);
    }

    private protected override void NotifyCommandsChanged()
    {
        DownloadCommand.NotifyCanExecuteChanged();
        PreviewFileCommand.NotifyCanExecuteChanged();
        UploadCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadTemplateAsync()
    {
        var collection = await GetAsync(_templatesHref);
        var href = collection.Entities is [var first, ..] ? first.Href ?? throw MissingAction() : throw MissingAction();
        var template = await GetAsync(href);
        _template = template;
        _properties = SirenProperties.Read<CardTemplateProperties>(template.Properties);
        IsLoaded = true;
        CanWrite = template.FindAction("upload-card-template") is not null;
        OnPropertyChanged(nameof(CharacterTemplateTitle));
        OnPropertyChanged(nameof(CharacterTemplateState));
        NotifyCommandsChanged();
    }

    private async Task DownloadAsync()
    {
        var href = _template?.FindLinkHref(SirenRelations.TemplateSource) ?? throw MissingAction();
        var content = await ApiClient.GetFileAsync(ApiBaseUrl, href, TemplateMediaType, Locale);
        if (await _dialogs.SaveTemplateAsync(content, TemplateFileName, Translate("cardTemplates.fileType")))
        {
            StatusMessage = Translate("cardTemplates.downloaded");
        }
    }

    private async Task PreviewFileAsync()
    {
        var action = _template?.FindAction("preview-card-template") ?? throw MissingAction();
        if (await PickCheckedTemplateAsync() is not { } content)
        {
            return;
        }

        var pdf = await WithTemplateErrorAsync(() => ApiClient.DownloadActionAsync(
            ApiBaseUrl, action, JsonSerializer.SerializeToElement(new { content }, JsonOptions), PdfMediaType, Locale));
        if (!await _dialogs.PreviewPdfAsync(pdf, PreviewFileName))
        {
            StatusMessage = Translate("cardTemplates.previewFailed");
        }
    }

    private async Task UploadAsync()
    {
        var action = _template?.FindAction("upload-card-template") ?? throw MissingAction();
        if (await PickCheckedTemplateAsync() is not { } content)
        {
            return;
        }

        var version = _properties?.Version ?? 0;
        await ReloadOnStaleAsync(() => WithTemplateErrorAsync(() =>
            SubmitAsync(action, JsonSerializer.SerializeToElement(new { version, content }, JsonOptions))));
        await LoadTemplateAsync();
        StatusMessage = Translate("cardTemplates.saved");
    }

    private async Task ResetAsync()
    {
        var action = _template?.FindAction("reset-card-template") ?? throw MissingAction();
        TemplateError = null;
        var confirmed = await _dialogs.ConfirmAsync(new ConfirmationRequest(
            Translate("skills.confirmTitle"),
            Translate("cardTemplates.confirmReset"),
            Translate("cardTemplates.reset"),
            Translate("common.cancel")));
        if (!confirmed)
        {
            return;
        }

        var version = _properties?.Version ?? 0;
        await ReloadOnStaleAsync(() =>
            SubmitAsync(action, JsonSerializer.SerializeToElement(new { version }, JsonOptions)));
        await LoadTemplateAsync();
        StatusMessage = Translate("cardTemplates.resetDone");
    }

    /// <returns>The chosen file, or <see langword="null"/> when the user cancels or the file is too large or empty.</returns>
    private async Task<byte[]?> PickCheckedTemplateAsync()
    {
        TemplateError = null;
        var content = await _dialogs.PickTemplateAsync();
        if (content is { Length: > MaximumTemplateByteCount })
        {
            StatusMessage = Localizer.TranslateApiError("errors.cardTemplateTooLarge", Locale);
            return null;
        }

        if (content is { Length: 0 })
        {
            StatusMessage = Localizer.TranslateApiError("errors.invalidCardTemplate", Locale);
            TemplateError = Translate("cardTemplates.emptyFile");
            return null;
        }

        return content;
    }

    private async Task<T> WithTemplateErrorAsync<T>(Func<Task<T>> request)
    {
        try
        {
            return await request();
        }
        catch (ManagementApiException failure) when (failure.Errors.TryGetValue(TemplateErrorKey, out var details))
        {
            TemplateError = string.Join(Environment.NewLine, details);
            throw;
        }
    }

    /// <summary>After a stale-version conflict, loads the latest template so the next attempt uses its version.</summary>
    private async Task ReloadOnStaleAsync(Func<Task<SirenEntity>> request)
    {
        try
        {
            await request();
        }
        catch (ManagementApiException failure) when (failure.ErrorCode == StaleVersionCode)
        {
            await LoadTemplateAsync();
            throw;
        }
    }

    private bool CanRun(string actionName) => !IsBusy && _template?.FindAction(actionName) is not null;
}
