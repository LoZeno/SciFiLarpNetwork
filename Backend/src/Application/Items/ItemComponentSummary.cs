namespace SciFiLarpNetwork.Backend.Application.Items;

/// <param name="Name">The component item's current name.</param>
/// <param name="IsArchived">Whether the component item has been archived since it was added.</param>
public sealed record ItemComponentSummary(Guid ItemId, string Name, int Quantity, bool IsArchived);
