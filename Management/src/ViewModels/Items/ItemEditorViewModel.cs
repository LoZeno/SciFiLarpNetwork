using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SciFiLarpNetwork.Management.Api.Items;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>
/// The item dialog, for a new item, an edit, or a clone. A chosen or removed picture is kept until Save, which sends the
/// details first and then the picture change. Cost and charges are checked here, so a typing mistake sends nothing.
/// </summary>
public sealed class ItemEditorViewModel : CatalogEditorViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Func<ItemEditorViewModel, Task<bool>> _submit;
    private readonly Func<Task<byte[]?>> _pickImage;
    private readonly int _maximumImageByteCount;
    private string _effect = string.Empty;
    private string _costText = "0";
    private string _chargesText = string.Empty;
    private DateTime? _expiresOn;
    private ItemChoice? _selectedType;
    private ItemChoice? _selectedRarity;
    private byte[]? _previewImage;
    private ItemImageChange _imageChange;
    private ItemComponentListItem? _selectedComponent;
    private Func<string, Task<IReadOnlyList<ItemChoice>>>? _searchComponents;
    private Func<ItemComponentEditorViewModel, Task>? _showComponentEditor;
    private bool _isCreated;

    internal ItemEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        IReadOnlyList<ItemChoice> types,
        IReadOnlyList<ItemChoice> rarities,
        bool isClone,
        int maximumImageByteCount,
        Func<Task<byte[]?>> pickImage,
        Func<ItemEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        Types = types;
        Rarities = rarities;
        IsClone = isClone;
        _maximumImageByteCount = maximumImageByteCount;
        _pickImage = pickImage;
        _submit = submit;
        _selectedType = types.Count > 0 ? types[0] : null;
        _selectedRarity = rarities.Count > 0 ? rarities[0] : null;
        ChooseImageCommand = new AsyncCommand(ChooseImageAsync, _ => ErrorMessage = Translate("networks.imageReadError"));
        RemoveImageCommand = new DelegateCommand(RemoveImage, () => HasPreviewImage);
        AddComponentCommand = new AsyncCommand(AddComponentAsync, _ => ErrorMessage = Translate("common.error"),
            () => _showComponentEditor is not null && !_isCreated);
        ChangeQuantityCommand = new AsyncCommand(ChangeQuantityAsync, _ => ErrorMessage = Translate("common.error"),
            () => _showComponentEditor is not null && !_isCreated && SelectedComponent is not null);
        RemoveComponentCommand = new DelegateCommand(RemoveComponent,
            () => _showComponentEditor is not null && !_isCreated && SelectedComponent is not null);
    }

    public IReadOnlyList<ItemChoice> Types { get; }

    public IReadOnlyList<ItemChoice> Rarities { get; }

    /// <summary>Whether the dialog creates a clone; it then shows a note that name and components must differ.</summary>
    public bool IsClone { get; }

    public AsyncCommand ChooseImageCommand { get; }

    public DelegateCommand RemoveImageCommand { get; }

    public AsyncCommand AddComponentCommand { get; }

    public AsyncCommand ChangeQuantityCommand { get; }

    public DelegateCommand RemoveComponentCommand { get; }

    /// <summary>The components a clone is created with; changed here, sent with the first save.</summary>
    public ObservableCollection<ItemComponentListItem> Components { get; } = [];

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
            NotifyComponentCommandsChanged();
        }
    }

    public string ComponentsLabel => Translate("items.components");

    public string AddComponentLabel => Translate("items.addComponent");

    public string ChangeQuantityLabel => Translate("items.changeQuantity");

    public string RemoveComponentLabel => Translate("items.removeComponent");

    public string TypeLabel => Translate("items.type");

    public string RarityLabel => Translate("items.rarity");

    public string EffectLabel => Translate("items.effect");

    public string CostLabel => Translate("items.cost");

    public string ChargesLabel => Translate("items.charges");

    public string ExpiresOnLabel => Translate("items.expiresOn");

    public string ImageLabel => Translate("items.image");

    public string ImageHelp => Translate("items.imageHelp");

    public string ChooseImageLabel => Translate("networks.chooseImage");

    public string RemoveImageLabel => Translate("networks.removeImage");

    public string CloneNote => Translate("items.cloneNote");

    public string Effect
    {
        get => _effect;
        set => Set(ref _effect, value);
    }

    public string CostText
    {
        get => _costText;
        set => Set(ref _costText, value);
    }

    public string ChargesText
    {
        get => _chargesText;
        set => Set(ref _chargesText, value);
    }

    public DateTime? ExpiresOn
    {
        get => _expiresOn;
        set
        {
            if (_expiresOn == value)
            {
                return;
            }

            _expiresOn = value;
            OnPropertyChanged();
        }
    }

    public ItemChoice? SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType == value)
            {
                return;
            }

            _selectedType = value;
            OnPropertyChanged();
        }
    }

    public ItemChoice? SelectedRarity
    {
        get => _selectedRarity;
        set
        {
            if (_selectedRarity == value)
            {
                return;
            }

            _selectedRarity = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The picture shown in the dialog: the chosen file, else the saved picture, else none.</summary>
    public byte[]? PreviewImage
    {
        get => _previewImage;
        private set
        {
            _previewImage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPreviewImage));
            RemoveImageCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasPreviewImage => PreviewImage is not null;

    internal ItemImageChange ImageChange => _imageChange;

    internal void Apply(ItemProperties item)
    {
        Name = item.Name;
        Description = item.Description ?? string.Empty;
        Effect = item.Effect ?? string.Empty;
        CostText = item.Cost.ToString(CultureInfo.CurrentCulture);
        ChargesText = item.Charges?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        ExpiresOn = item.ExpiresOn is { } date ? date.ToDateTime(TimeOnly.MinValue) : null;
        SelectedType = Types.FirstOrDefault(type => type.Id == item.ItemTypeId) ?? SelectedType;
        SelectedRarity = Rarities.FirstOrDefault(rarity => rarity.Id == item.RarityId) ?? SelectedRarity;
    }

    internal void ShowSavedImage(byte[]? image)
    {
        if (_imageChange == ItemImageChange.None)
        {
            PreviewImage = image;
        }
    }

    internal void MarkImageChangeSaved() => _imageChange = ItemImageChange.None;

    /// <summary>
    /// Lets the dialog change the components a clone starts with, because two items cannot have the same components.
    /// </summary>
    internal void EditComponents(
        IEnumerable<ItemComponentProperties> components,
        Func<string, Task<IReadOnlyList<ItemChoice>>> search,
        Func<ItemComponentEditorViewModel, Task> showComponentEditor)
    {
        _searchComponents = search;
        _showComponentEditor = showComponentEditor;
        foreach (var component in components)
        {
            Components.Add(ToComponentItem(component.ItemId, component.Name, component.Quantity));
        }

        OnPropertyChanged(nameof(EditsComponents));
        NotifyComponentCommandsChanged();
    }

    /// <summary>Whether the dialog shows the components; only a clone changes them here.</summary>
    public bool EditsComponents => _showComponentEditor is not null;

    /// <summary>After the item is created, later saves change only its details, so the components are fixed.</summary>
    internal void MarkCreated()
    {
        _isCreated = true;
        NotifyComponentCommandsChanged();
    }

    internal IReadOnlyList<ItemComponentProperties> ComponentsToSave() =>
        [.. Components.Select(component => new ItemComponentProperties(component.ItemId, component.Name, component.Quantity, false))];

    /// <summary>Checks the typed numbers; shows the error and returns <see langword="false"/> when one is not valid.</summary>
    internal bool ValidateNumbers()
    {
        if (!int.TryParse(CostText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out _))
        {
            ErrorMessage = Localizer.TranslateApiError("errors.invalidItemCost", Locale);
            return false;
        }

        if (ChargesText.Trim().Length > 0 && !int.TryParse(ChargesText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out _))
        {
            ErrorMessage = Localizer.TranslateApiError("errors.invalidCharges", Locale);
            return false;
        }

        return true;
    }

    /// <summary>The details to save; <paramref name="version"/> is set for an edit.</summary>
    /// <param name="components">The components a new item or clone starts with.</param>
    internal JsonElement CreateBody(long? version, IReadOnlyList<ItemComponentProperties>? components, Guid? clonedFromItemId) =>
        JsonSerializer.SerializeToElement(
            new
            {
                version,
                name = Name,
                itemTypeId = SelectedType?.Id ?? Guid.Empty,
                rarityId = SelectedRarity?.Id ?? Guid.Empty,
                description = Blank(Description),
                effect = Blank(Effect),
                cost = int.Parse(CostText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture),
                charges = ChargesText.Trim().Length == 0 ? (int?)null : int.Parse(ChargesText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture),
                expiresOn = ExpiresOn is { } date ? DateOnly.FromDateTime(date) : (DateOnly?)null,
                components = components?.Select(component => new { itemId = component.ItemId, quantity = component.Quantity }).ToArray(),
                clonedFromItemId
            },
            JsonOptions);

    internal JsonElement CreateImageBody(long version) =>
        JsonSerializer.SerializeToElement(
            _imageChange == ItemImageChange.Upload ? new { version, content = PreviewImage } : (object)new { version },
            JsonOptions);

    private protected override Task<bool> SubmitAsync() => ValidateNumbers() ? _submit(this) : Task.FromResult(false);

    private static string? Blank(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private async Task ChooseImageAsync()
    {
        if (await _pickImage() is not { } image)
        {
            return;
        }

        if (image.Length > _maximumImageByteCount)
        {
            ErrorMessage = Localizer.TranslateApiError("errors.imageTooLarge", Locale);
            return;
        }

        ErrorMessage = null;
        _imageChange = ItemImageChange.Upload;
        PreviewImage = image;
    }

    private void RemoveImage()
    {
        _imageChange = ItemImageChange.Remove;
        PreviewImage = null;
    }

    private async Task AddComponentAsync()
    {
        var editor = new ItemComponentEditorViewModel(Localizer, Locale, "items.addComponent", null, 1, _searchComponents,
            editor =>
            {
                if (Components.Any(component => component.ItemId == editor.SelectedItem!.Id))
                {
                    editor.ErrorMessage = Localizer.TranslateApiError("errors.componentAlreadyAdded", Locale);
                    return Task.FromResult(false);
                }

                Components.Add(ToComponentItem(editor.SelectedItem!.Id!.Value, editor.SelectedItem.Name, editor.Quantity));
                return Task.FromResult(true);
            });
        await editor.SearchCommand.ExecuteAsync();
        await _showComponentEditor!(editor);
    }

    private async Task ChangeQuantityAsync()
    {
        var component = SelectedComponent!;
        var editor = new ItemComponentEditorViewModel(Localizer, Locale, "items.changeQuantity", component.Name, component.Quantity,
            null,
            editor =>
            {
                Components[Components.IndexOf(component)] = ToComponentItem(component.ItemId, component.Name, editor.Quantity);
                return Task.FromResult(true);
            });
        await _showComponentEditor!(editor);
    }

    private void RemoveComponent() => Components.Remove(SelectedComponent!);

    private static ItemComponentListItem ToComponentItem(Guid itemId, string name, int quantity) =>
        new(itemId, name, quantity, false, $"{quantity} × {name}");

    private void NotifyComponentCommandsChanged()
    {
        AddComponentCommand.NotifyCanExecuteChanged();
        ChangeQuantityCommand.NotifyCanExecuteChanged();
        RemoveComponentCommand.NotifyCanExecuteChanged();
    }

    private void Set(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}
