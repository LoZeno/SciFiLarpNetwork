namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <param name="DisplayName">The running number, name, and dates, with localized tags for closed and archived events.</param>
public sealed record LarpEventListItem(
    Guid EventId,
    string Name,
    bool IsOpen,
    bool IsArchived,
    string Href,
    string DisplayName);
