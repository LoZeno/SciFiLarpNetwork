using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Items;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Items;

/// <summary>Plays the user's part in each item dialog through the supplied callbacks.</summary>
internal sealed class FakeItemDialogs : IItemDialogService
{
    public Func<ItemEditorViewModel, Task> OnItemEditor { get; init; } = _ => Task.CompletedTask;

    public Func<ItemComponentEditorViewModel, Task> OnComponentEditor { get; init; } = _ => Task.CompletedTask;

    public Func<ItemCatalogViewModel, Task> OnCatalog { get; init; } = _ => Task.CompletedTask;

    public Func<ItemCatalogEntryEditorViewModel, Task> OnCatalogEntryEditor { get; init; } = _ => Task.CompletedTask;

    public bool ConfirmResult { get; set; } = true;

    public bool OpenPdfResult { get; init; } = true;

    /// <summary>The file the user picks; <see langword="null"/> means the user cancels the picker.</summary>
    public byte[]? PickedImage { get; set; }

    public ItemEditorViewModel? LastEditor { get; private set; }

    public ItemComponentEditorViewModel? LastComponentEditor { get; private set; }

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public List<byte[]> OpenedPdfs { get; } = [];

    public Task ShowItemEditorAsync(ItemEditorViewModel editor)
    {
        LastEditor = editor;
        return OnItemEditor(editor);
    }

    public Task ShowComponentEditorAsync(ItemComponentEditorViewModel editor)
    {
        LastComponentEditor = editor;
        return OnComponentEditor(editor);
    }

    public ItemCatalogViewModel? LastCatalog { get; private set; }

    public Task ShowCatalogAsync(ItemCatalogViewModel catalog)
    {
        LastCatalog = catalog;
        return OnCatalog(catalog);
    }

    public Task ShowCatalogEntryEditorAsync(ItemCatalogEntryEditorViewModel editor) => OnCatalogEntryEditor(editor);

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }

    public Task<byte[]?> PickImageAsync() => Task.FromResult(PickedImage);

    public Task<bool> OpenPdfAsync(byte[] pdf, string fileName)
    {
        OpenedPdfs.Add(pdf);
        return Task.FromResult(OpenPdfResult);
    }
}
