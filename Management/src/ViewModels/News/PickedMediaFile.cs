namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <param name="FileName">The file's name without its folder.</param>
public sealed record PickedMediaFile(string FileName, byte[] Content);
