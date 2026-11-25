namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <param name="IsEnabled">Disabled networks are listed only so existing stories can be shown and kept.</param>
public sealed record NetworkChoice(Guid NetworkId, string DisplayName, bool IsEnabled);
