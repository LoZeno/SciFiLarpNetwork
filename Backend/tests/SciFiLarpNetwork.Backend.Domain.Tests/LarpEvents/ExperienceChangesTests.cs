using SciFiLarpNetwork.Backend.Domain.LarpEvents;

namespace SciFiLarpNetwork.Backend.Tests.Domain.LarpEvents;

public sealed class ExperienceChangesTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    public void OnlyZeroIsNotAValidAmount(int amount, bool expected) =>
        Assert.Equal(expected, ExperienceChanges.IsValidAmount(amount));

    [Theory]
    [InlineData(10, 5, 15)]
    [InlineData(10, -10, 0)]
    [InlineData(int.MaxValue - 1, 1, int.MaxValue)]
    public void ApplyAddsTheAmountWithinRange(int experiencePoints, int amount, int expected) =>
        Assert.Equal(expected, ExperienceChanges.Apply(experiencePoints, amount));

    [Theory]
    [InlineData(10, -11)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(0, int.MinValue)]
    public void ApplyOutsideTheRangeGivesNull(int experiencePoints, int amount) =>
        Assert.Null(ExperienceChanges.Apply(experiencePoints, amount));
}
