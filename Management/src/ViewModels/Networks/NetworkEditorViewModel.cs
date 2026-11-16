using System.Text.Json;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Networks;

/// <summary>
/// The network dialog. A chosen or removed background image is kept until Save, which sends the details first and
/// then the image change.
/// </summary>
public sealed class NetworkEditorViewModel : CatalogEditorViewModel
{
    /// <summary>The upload limit enforced by the API, checked here too so a too-large file is never sent.</summary>
    public const int MaximumImageByteCount = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Func<NetworkEditorViewModel, Task<bool>> _submit;
    private readonly Func<Task<byte[]?>> _pickImage;
    private string _code = string.Empty;
    private NetworkThemeChoice? _selectedTheme;
    private byte[]? _previewImage;
    private NetworkImageChange _imageChange;

    internal NetworkEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        IReadOnlyList<NetworkThemeChoice> themes,
        bool isCodeEditable,
        Func<Task<byte[]?>> pickImage,
        Func<NetworkEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        Themes = themes;
        IsCodeEditable = isCodeEditable;
        _pickImage = pickImage;
        _submit = submit;
        _selectedTheme = themes.Count > 0 ? themes[0] : null;
        ChooseImageCommand = new AsyncCommand(ChooseImageAsync, _ => ErrorMessage = Translate("networks.imageReadError"));
        RemoveImageCommand = new DelegateCommand(RemoveImage, () => HasPreviewImage);
    }

    public IReadOnlyList<NetworkThemeChoice> Themes { get; }

    /// <summary>A code can only be typed when the network is created; it is fixed afterwards.</summary>
    public bool IsCodeEditable { get; }

    public AsyncCommand ChooseImageCommand { get; }

    public DelegateCommand RemoveImageCommand { get; }

    public string CodeLabel => Translate("networks.code");

    public string CodeHelp => Translate("networks.codeHelp");

    public string ThemeLabel => Translate("networks.theme");

    public string BackgroundImageLabel => Translate("networks.backgroundImage");

    public string ImageHelp => Translate("networks.imageHelp");

    public string ChooseImageLabel => Translate("networks.chooseImage");

    public string RemoveImageLabel => Translate("networks.removeImage");

    public string ImageStatus => Translate(HasPreviewImage ? "networks.customImage" : "networks.themeImage");

    public string Code
    {
        get => _code;
        set
        {
            if (_code == value)
            {
                return;
            }

            _code = value;
            OnPropertyChanged();
        }
    }

    public NetworkThemeChoice? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value)
            {
                return;
            }

            _selectedTheme = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The image shown in the dialog: the chosen file, else the saved image, else none.</summary>
    public byte[]? PreviewImage
    {
        get => _previewImage;
        private set
        {
            _previewImage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPreviewImage));
            OnPropertyChanged(nameof(ImageStatus));
            RemoveImageCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasPreviewImage => PreviewImage is not null;

    internal NetworkImageChange ImageChange => _imageChange;

    internal void ShowSavedImage(byte[]? image)
    {
        if (_imageChange == NetworkImageChange.None)
        {
            PreviewImage = image;
        }
    }

    internal void SelectTheme(string themeName) =>
        SelectedTheme = Themes.FirstOrDefault(theme => theme.Name == themeName) ?? SelectedTheme;

    internal void MarkImageChangeSaved() => _imageChange = NetworkImageChange.None;

    internal JsonElement CreateDetailsBody(long? version) =>
        JsonSerializer.SerializeToElement(
            new
            {
                version,
                code = IsCodeEditable && version is null ? Code : null,
                name = Name,
                description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                theme = SelectedTheme?.Name
            },
            JsonOptions);

    internal JsonElement CreateImageBody(long version) =>
        JsonSerializer.SerializeToElement(
            _imageChange == NetworkImageChange.Upload ? new { version, content = PreviewImage } : (object)new { version },
            JsonOptions);

    private protected override Task<bool> SubmitAsync() => _submit(this);

    private async Task ChooseImageAsync()
    {
        if (await _pickImage() is not { } image)
        {
            return;
        }

        if (image.Length > MaximumImageByteCount)
        {
            ErrorMessage = Localizer.TranslateApiError("errors.imageTooLarge", Locale);
            return;
        }

        ErrorMessage = null;
        _imageChange = NetworkImageChange.Upload;
        PreviewImage = image;
    }

    private void RemoveImage()
    {
        _imageChange = NetworkImageChange.Remove;
        PreviewImage = null;
    }
}
