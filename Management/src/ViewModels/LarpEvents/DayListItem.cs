namespace SciFiLarpNetwork.Management.ViewModels.LarpEvents;

/// <param name="DayId">The day, or <see langword="null"/> for the entry that shows every day.</param>
/// <param name="Experience">The experience the day can give when the event closes.</param>
public sealed record DayListItem(Guid? DayId, DateOnly? Date, int? Experience, string DisplayName);
