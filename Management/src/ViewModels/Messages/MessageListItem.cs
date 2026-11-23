namespace SciFiLarpNetwork.Management.ViewModels.Messages;

/// <param name="DisplayName">The time, sender, title, and recipient count.</param>
public sealed record MessageListItem(Guid MessageId, string Href, string DisplayName);
