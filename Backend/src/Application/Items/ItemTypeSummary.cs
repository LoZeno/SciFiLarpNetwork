namespace SciFiLarpNetwork.Backend.Application.Items;

public sealed record ItemTypeSummary(Guid ItemTypeId, string Name, bool IsArchived, long Version);
