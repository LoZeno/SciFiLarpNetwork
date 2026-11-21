namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <param name="DayId">The day, or <see langword="null"/> for the entry that shows every day.</param>
public sealed record DayListItem(Guid? DayId, DateOnly? Date, string DisplayName);
