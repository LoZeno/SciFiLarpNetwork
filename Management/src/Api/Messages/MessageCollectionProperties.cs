namespace SciFiLarpNetwork.Management.Api.Messages;

internal sealed record MessageCollectionProperties(
    string Title,
    string Description,
    Guid? CharacterId,
    int PageNumber,
    int PageCount,
    long TotalCount,
    int MaximumRecipients,
    int MaximumTitleLength,
    int MaximumBodyLength);
