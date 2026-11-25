namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>Shows the news screen's dialogs. Each task completes when its dialog closes.</summary>
public interface INewsDialogService
{
    Task ShowStoryEditorAsync(NewsEditorViewModel editor);

    Task ShowMediaLibraryAsync(NewsMediaLibraryViewModel library);

    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>
    /// Lets the user choose a picture or video file and returns it, reading at most one byte more than
    /// <paramref name="maximumByteCount"/>; <see langword="null"/> when the user cancels.
    /// </summary>
    Task<PickedMediaFile?> PickMediaFileAsync(long maximumByteCount);
}
