namespace SciFiLarpNetwork.Backend.Application.LarpEvents;

/// <summary>The editable details of an event. Days, status, and archiving have their own commands.</summary>
public sealed record LarpEventDefinition(string Name, string? Description);
