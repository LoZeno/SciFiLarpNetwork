using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

/// <summary>Plays the user's part in the news dialogs.</summary>
internal sealed class FakeNewsDialogs : INewsDialogService
{
    public Func<NewsEditorViewModel, Task> OnStoryEditor { get; init; } = _ => Task.CompletedTask;

    public Func<NewsMediaLibraryViewModel, Task> OnMediaLibrary { get; set; } = _ => Task.CompletedTask;

    public bool ConfirmResult { get; init; } = true;

    public PickedMediaFile? PickedFile { get; set; }

    public NewsEditorViewModel? LastEditor { get; private set; }

    public NewsMediaLibraryViewModel? LastLibrary { get; private set; }

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public long? LastPickLimit { get; private set; }

    public Task ShowStoryEditorAsync(NewsEditorViewModel editor)
    {
        LastEditor = editor;
        return OnStoryEditor(editor);
    }

    public Task ShowMediaLibraryAsync(NewsMediaLibraryViewModel library)
    {
        LastLibrary = library;
        return OnMediaLibrary(library);
    }

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }

    public Task<PickedMediaFile?> PickMediaFileAsync(long maximumByteCount)
    {
        LastPickLimit = maximumByteCount;
        return Task.FromResult(PickedFile);
    }
}
