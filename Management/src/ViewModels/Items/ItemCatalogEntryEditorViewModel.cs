using System.Text.Json;
using SciFiLarpNetwork.Management.Localization;

namespace SciFiLarpNetwork.Management.ViewModels.Items;

/// <summary>The dialog that names a new item type or rarity, or renames one. Only the name is used.</summary>
public sealed class ItemCatalogEntryEditorViewModel : CatalogEditorViewModel
{
    private readonly Func<ItemCatalogEntryEditorViewModel, Task<bool>> _submit;

    internal ItemCatalogEntryEditorViewModel(
        IManagementLocalizer localizer,
        string locale,
        string titleKey,
        Func<ItemCatalogEntryEditorViewModel, Task<bool>> submit)
        : base(localizer, locale, titleKey)
    {
        _submit = submit;
    }

    internal JsonElement CreateBody(long? version) =>
        version is { } current
            ? JsonSerializer.SerializeToElement(new { version = current, name = Name })
            : JsonSerializer.SerializeToElement(new { name = Name });

    private protected override Task<bool> SubmitAsync() => _submit(this);
}
