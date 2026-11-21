namespace SciFiLarpNetwork.Backend.Infrastructure.LarpEvents.LarpEventDocuments;

/// <param name="Experience">The experience the day can give when the event closes; <see langword="null"/> for none.</param>
public sealed record LarpEventDay(Guid DayId, DateOnly Date, int? Experience);
