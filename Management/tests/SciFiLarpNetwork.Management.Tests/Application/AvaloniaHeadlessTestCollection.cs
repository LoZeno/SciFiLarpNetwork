namespace SciFiLarpNetwork.Management.Tests.Application;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AvaloniaHeadlessTestGroup : ICollectionFixture<AvaloniaHeadlessFixture>
{
    public const string Name = "Avalonia headless";
}
