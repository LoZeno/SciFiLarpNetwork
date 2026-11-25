namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
