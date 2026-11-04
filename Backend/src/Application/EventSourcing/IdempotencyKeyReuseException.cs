namespace SciFiLarpNetwork.Backend.Application.EventSourcing;

public sealed class IdempotencyKeyReuseException(Guid commandId)
    : Exception($"Command ID '{commandId}' was already used for a different append request.")
{
    public Guid CommandId { get; } = commandId;
}
