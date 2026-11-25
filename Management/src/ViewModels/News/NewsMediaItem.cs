namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>A library picture or video, as the screen and dialogs show it.</summary>
/// <param name="ContentHref">The advertised address of the file.</param>
/// <param name="RemoveAction">The advertised removal, offered only to administrators.</param>
/// <param name="PictureContent">The downloaded picture; <see langword="null"/> for a video or while not loaded.</param>
public sealed record NewsMediaItem(
    Guid MediaId,
    string Name,
    string MediaType,
    bool IsVideo,
    long ByteCount,
    int? Width,
    int? Height,
    string? ContentHref,
    Common.Siren.SirenAction? RemoveAction,
    byte[]? PictureContent = null)
{
    public bool IsPicture => !IsVideo;
}
