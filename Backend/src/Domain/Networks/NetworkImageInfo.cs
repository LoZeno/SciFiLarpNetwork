namespace SciFiLarpNetwork.Backend.Domain.Networks;

/// <summary>The format and pixel size read from an image file's header.</summary>
public sealed record NetworkImageInfo(string MediaType, int Width, int Height);
