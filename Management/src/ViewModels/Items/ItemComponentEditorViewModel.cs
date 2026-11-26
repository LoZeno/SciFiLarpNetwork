using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>
/// The component dialog: to add a component it offers the items found by a search and a quantity; to change a quantity
/// it offers only the quantity.
/// </summary>
public sealed class ItemComponentEditorViewModel : CatalogEditorViewModel
{
    private readonly Func<string, Task<IReadOnlyList<ItemChoice>>>? _search;
    private readonly Func<ItemComponentEditorViewModel, Task<bool>> _submit;
    private string _quantityText;
    private string _searchText = string.Empty;
    private ItemChoice? _selectedItem;

    internal ItemComponentEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        string? componentName,
        int quantity,
        Func<string, Task<IReadOnlyList<ItemChoice>>>? search,
        Func<ItemComponentEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        ComponentName = componentName;
        _quantityText = quantity.ToString(CultureInfo.CurrentCulture);
        _search = search;
        _submit = submit;
        SearchCommand = new AsyncCommand(SearchCoreAsync, _ => ErrorMessage = Translate("common.error"), () => _search is not null);
    }

    /// <summary>The component whose quantity changes, or <see langword="null"/> when one is being added.</summary>
    public string? ComponentName { get; }

    public bool IsAdding => ComponentName is null;

    public AsyncCommand SearchCommand { get; }

    public ObservableCollection<ItemChoice> Choices { get; } = [];

    public string ComponentLabel => Translate("items.component");

    public string QuantityLabel => Translate("items.quantity");

    public string SearchLabel => Translate("items.search");

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (_quantityText == value)
            {
                return;
            }

            _quantityText = value;
            OnPropertyChanged();
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
        }
    }

    public ItemChoice? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value)
            {
                return;
            }

            _selectedItem = value;
            OnPropertyChanged();
        }
    }

    internal int Quantity => int.Parse(QuantityText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture);

    internal JsonElement CreateAddBody(long version) =>
        JsonSerializer.SerializeToElement(new { version, itemId = SelectedItem?.Id ?? Guid.Empty, quantity = Quantity });

    internal JsonElement CreateQuantityBody(long version) => JsonSerializer.SerializeToElement(new { version, quantity = Quantity });

    private protected override Task<bool> SubmitAsync()
    {
        if (!int.TryParse(QuantityText.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out _))
        {
            ErrorMessage = Localizer.TranslateApiError("errors.invalidQuantity", Locale);
            return Task.FromResult(false);
        }

        if (IsAdding && SelectedItem is null)
        {
            ErrorMessage = Localizer.TranslateApiError("errors.unknownComponent", Locale);
            return Task.FromResult(false);
        }

        return _submit(this);
    }

    private async Task SearchCoreAsync()
    {
        var found = await _search!(SearchText);
        Choices.Clear();
        foreach (var choice in found)
        {
            Choices.Add(choice);
        }

        SelectedItem = Choices.FirstOrDefault();
    }
}
