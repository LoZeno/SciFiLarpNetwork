namespace SciFiLarpNetwork.Management.Api.Items;

internal sealed record ItemComponentProperties(Guid ItemId, string Name, int Quantity, bool IsArchived);
