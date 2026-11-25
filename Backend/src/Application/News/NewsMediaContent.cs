namespace SciFiLarpNetwork.Backend.Application.News;

/// <param name="Content">A seekable stream over the stored file, so parts of it can be served; the caller disposes it.</param>
public sealed record NewsMediaContent(string MediaType, Stream Content);
