namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <param name="DisplayName">The character, player, registered days, and experience given at the event.</param>
public sealed record ParticipantListItem(
    Guid CharacterId,
    string CharacterName,
    IReadOnlyList<Guid> DayIds,
    int ExperiencePoints,
    long ExperienceTotal,
    string DisplayName);
