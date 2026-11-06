namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public sealed record AppendResult(long StreamVersion, IReadOnlyList<Guid> EventIds, bool WasDuplicate);
