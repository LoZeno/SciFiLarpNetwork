namespace SciFiLarpNetwork.Management.ViewModels.Messages;

/// <summary>The limits the API advertises with the message collection.</summary>
public sealed record MessageLimits(int MaximumRecipients, int MaximumTitleLength, int MaximumBodyLength);
