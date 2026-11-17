namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <param name="NetworkId">The non-default network, or <see langword="null"/> for none.</param>
public sealed record NetworkChoice(Guid? NetworkId, string Title);
