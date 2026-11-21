namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The editable details of an event. Days, status, and archiving have their own commands.</summary>
/// <param name="BaseExperience">The experience points every chosen participant can get when the event closes.</param>
public sealed record LarpEventDefinition(string Name, string? Description, int? BaseExperience = null);
