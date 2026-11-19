namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <param name="Skills">The skills bought through the talent, in name order.</param>
public sealed record CardTalent(string Name, IReadOnlyList<CardSkill> Skills);
