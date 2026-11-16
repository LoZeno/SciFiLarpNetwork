using SciFiLarpNetwork.Backend.Domain.Networks;

namespace SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;

/// <param name="BackgroundImageId">The uploaded background image, or <see langword="null"/> for the theme's image.</param>
public sealed record NetworkDocument(
    Guid Id,
    string Code,
    string Name,
    string NormalizedName,
    string? Description,
    NetworkTheme Theme,
    Guid? BackgroundImageId,
    string? BackgroundImageMediaType,
    int BackgroundImageByteCount,
    bool IsEnabled,
    bool IsDefault)
{
    /// <summary>The network stream version, set by Marten for aggregate projection documents.</summary>
    public long Version { get; set; }
}
