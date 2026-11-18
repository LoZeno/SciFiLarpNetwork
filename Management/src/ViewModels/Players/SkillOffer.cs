namespace SciFiLarpNetwork.Management.ViewModels.Players;

/// <summary>A skill the character can buy through one of its talents.</summary>
public sealed record SkillOffer(Guid SkillId, Guid TalentId, string Title);
