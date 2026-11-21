namespace SciFiLarpNetwork.Backend.Domain.LarpEvents;

/// <summary>Rules for an experience change given to a participant of an event.</summary>
public static class ExperienceChanges
{
    public static bool IsValidAmount(int amount) => amount != 0;

    /// <summary>An event's base experience or a day's experience: blank, or a whole number of 0 or more.</summary>
    public static bool IsValidValue(int? value) => value is null or >= 0;

    /// <returns>
    /// The experience points after the change, or <see langword="null"/> when they would leave the range 0 to
    /// <see cref="int.MaxValue"/>.
    /// </returns>
    public static int? Apply(int experiencePoints, int amount)
    {
        var changed = (long)experiencePoints + amount;
        return changed is < 0 or > int.MaxValue ? null : (int)changed;
    }
}
