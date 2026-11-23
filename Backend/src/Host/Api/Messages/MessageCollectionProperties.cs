namespace SciFiLarpNetwork.Backend.Api.Messages;

/// <param name="CharacterId">The character whose incoming messages are listed; <see langword="null"/> for all messages.</param>
/// <param name="MaximumRecipients">The most recipients one message can hold.</param>
public sealed record MessageCollectionProperties(
    string Title,
    string Description,
    Guid? CharacterId,
    int PageNumber,
    int PageCount,
    long TotalCount,
    int MaximumRecipients,
    int MaximumTitleLength,
    int MaximumBodyLength);
