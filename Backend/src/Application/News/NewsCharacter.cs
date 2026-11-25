namespace SciFiLarpNetwork.Backend.Application.News;

/// <summary>A story's author as shown now; one that cannot be found has no number or name.</summary>
public sealed record NewsCharacter(Guid CharacterId, bool IsKnown, int? Number, string? Name, bool IsArchived);
