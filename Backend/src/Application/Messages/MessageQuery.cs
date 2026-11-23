namespace SciFiLarpNetwork.Backend.Application.Messages;

/// <param name="CharacterId">Lists only the incoming messages of this character; <see langword="null"/> for all.</param>
/// <param name="PageNumber">The one-based page of <see cref="PageSize"/> messages, newest first.</param>
public sealed record MessageQuery(Guid? CharacterId, int PageNumber)
{
    public const int PageSize = 50;
}
