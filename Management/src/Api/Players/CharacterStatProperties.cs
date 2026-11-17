namespace SciFiLarpNetwork.Management.Api.Players;

internal sealed record CharacterStatProperties(Guid StatId, string Name, string Type, bool IsArchived, long Version);
