using JasperFx;
using JasperFx.Events;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Domain.Events.NetworkEvents;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Domain.Skills;
using SciFiLarpNetwork.Backend.Infrastructure.Networks.NetworkDocuments;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Networks;

[Collection("Backend database integration")]
public sealed class NetworkServiceTests : IAsyncLifetime
{
    private readonly AdministratorAuthenticationFactory _factory = new();

    private INetworkService Networks => _factory.Services.GetRequiredService<INetworkService>();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreatedNetworkKeepsEveryField()
    {
        var result = await Networks.CreateNetworkAsync(
            "  Outer-Rim ",
            new NetworkDefinition("  Outer Rim  ", " Frontier relay ", NetworkTheme.Amber),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new NetworkSummary(result.Value!.NetworkId, "outer-rim", "Outer Rim", "Frontier relay", NetworkTheme.Amber, null, true, false, 1),
            result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-rim")]
    [InlineData("outer rim")]
    public async Task MalformedCodeIsRejected(string code)
    {
        var result = await Networks.CreateNetworkAsync(code, Definition("Outer Rim"), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.InvalidCode, result.Outcome);
    }

    [Fact]
    public async Task BlankNameIsRejected()
    {
        var result = await Networks.CreateNetworkAsync("rim", Definition(" "), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.InvalidName, result.Outcome);
    }

    [Fact]
    public async Task UndefinedThemeIsRejected()
    {
        var result = await Networks.CreateNetworkAsync(
            "rim", new NetworkDefinition("Outer Rim", null, (NetworkTheme)99), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.InvalidTheme, result.Outcome);
    }

    [Fact]
    public async Task DuplicateCodeIsRejectedIgnoringCase()
    {
        await CreateAsync("rim", "Outer Rim");

        var result = await Networks.CreateNetworkAsync("RIM", Definition("Inner Rim"), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.CodeAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task DuplicateNameIsRejectedIgnoringCase()
    {
        await CreateAsync("rim", "Outer Rim");

        var result = await Networks.CreateNetworkAsync("rim-2", Definition("OUTER RIM"), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task EditChangesTheDetailsButNotTheCode()
    {
        var network = await CreateAsync("rim", "Outer Rim");

        var result = await Networks.UpdateNetworkAsync(
            network.NetworkId, network.Version, new NetworkDefinition("Far Rim", "Remote", NetworkTheme.Purple), TestContext.Current.CancellationToken);

        Assert.Equal(
            network with { Name = "Far Rim", Description = "Remote", Theme = NetworkTheme.Purple, Version = 2 },
            result.Value);
    }

    [Fact]
    public async Task UnchangedEditKeepsTheVersion()
    {
        var network = await CreateAsync("rim", "Outer Rim");

        var result = await Networks.UpdateNetworkAsync(network.NetworkId, network.Version, Definition("Outer Rim"), TestContext.Current.CancellationToken);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task EditCannotTakeAnotherNetworksName()
    {
        await CreateAsync("core", "Core");
        var network = await CreateAsync("rim", "Outer Rim");

        var result = await Networks.UpdateNetworkAsync(network.NetworkId, network.Version, Definition("core"), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NameAlreadyUsed, result.Outcome);
    }

    [Fact]
    public async Task StaleEditIsRejected()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.UpdateNetworkAsync(network.NetworkId, network.Version, Definition("Far Rim"), TestContext.Current.CancellationToken);

        var result = await Networks.UpdateNetworkAsync(network.NetworkId, network.Version, Definition("Near Rim"), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.StaleVersion, result.Outcome);
    }

    [Fact]
    public async Task EditOfAnUnknownNetworkIsNotFound()
    {
        var result = await Networks.UpdateNetworkAsync(Guid.NewGuid(), 1, Definition("Outer Rim"), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task DisabledNetworkIsHiddenFromTheDefaultList()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await CreateAsync("core", "Core");
        await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var networks = await Networks.GetNetworksAsync(includeDisabled: false, TestContext.Current.CancellationToken);

        Assert.Equal(["core"], networks.Select(item => item.Code));
    }

    [Fact]
    public async Task DisabledNetworksCanBeListed()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await CreateAsync("core", "Core");
        await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var networks = await Networks.GetNetworksAsync(includeDisabled: true, TestContext.Current.CancellationToken);

        Assert.Equal(["core:True", "rim:False"], networks.Select(item => $"{item.Code}:{item.IsEnabled}"));
    }

    [Fact]
    public async Task EnablingRestoresADisabledNetwork()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Networks.EnableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(new { IsEnabled = true, Version = 3L }, new { result.Value!.IsEnabled, result.Value.Version });
    }

    [Fact]
    public async Task EnablingAnEnabledNetworkChangesNothing()
    {
        var network = await CreateAsync("rim", "Outer Rim");

        var result = await Networks.EnableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(1L, result.Value!.Version);
    }

    [Fact]
    public async Task DisablingADisabledNetworkChangesNothing()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(2L, result.Value!.Version);
    }

    [Fact]
    public async Task DisablingKeepsTheNetworkDetails()
    {
        var network = await CreateAsync("rim", "Outer Rim");

        var result = await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(network with { IsEnabled = false, Version = 2 }, result.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnablingOrDisablingAnUnknownNetworkIsNotFound(bool enable)
    {
        var result = enable
            ? await Networks.EnableNetworkAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)
            : await Networks.DisableNetworkAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task NewNetworksAreNotTheDefault()
    {
        var network = await CreateAsync("rim", "Outer Rim");

        Assert.False(network.IsDefault);
    }

    [Fact]
    public async Task MakeDefaultMarksTheNetwork()
    {
        var network = await CreateAsync("rim", "Outer Rim");

        var result = await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.True(result.Value!.IsDefault);
    }

    [Fact]
    public async Task MakeDefaultMovesTheDefaultFromThePreviousNetwork()
    {
        var first = await CreateAsync("rim", "Outer Rim");
        var second = await CreateAsync("core", "Core");
        await Networks.MakeDefaultNetworkAsync(first.NetworkId, TestContext.Current.CancellationToken);

        await Networks.MakeDefaultNetworkAsync(second.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(["core:True", "rim:False"], await DefaultFlagsAsync());
    }

    [Fact]
    public async Task MakingTheDefaultAgainChangesNothing()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(2L, result.Value!.Version);
    }

    [Fact]
    public async Task DisabledNetworkCannotBeMadeDefault()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NetworkDisabled, result.Outcome);
    }

    [Fact]
    public async Task UnknownNetworkCannotBeMadeDefault()
    {
        await CreateAsync("rim", "Outer Rim");

        var result = await Networks.MakeDefaultNetworkAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task DefaultNetworkCannotBeDisabled()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        var result = await Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        Assert.Equal(NetworkOutcome.DefaultNetworkCannotBeDisabled, result.Outcome);
    }

    [Fact]
    public async Task ParallelDefaultChangesLetOnlyOneSucceed()
    {
        var first = await CreateAsync("rim", "Outer Rim");
        var second = await CreateAsync("core", "Core");
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Networks.MakeDefaultNetworkAsync(first.NetworkId, TestContext.Current.CancellationToken)),
            Task.Run(() => Networks.MakeDefaultNetworkAsync(second.NetworkId, TestContext.Current.CancellationToken)));
        _factory.Clock.ReadBarrier = null;

        Assert.Equal([NetworkOutcome.Succeeded, NetworkOutcome.StaleVersion], results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task ParallelDefaultChangesAfterAnEarlierDefaultLetOnlyOneSucceed()
    {
        var first = await CreateAsync("rim", "Outer Rim");
        var second = await CreateAsync("core", "Core");
        var third = await CreateAsync("mid", "Mid Rim");
        await Networks.MakeDefaultNetworkAsync(third.NetworkId, TestContext.Current.CancellationToken);
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Networks.MakeDefaultNetworkAsync(first.NetworkId, TestContext.Current.CancellationToken)),
            Task.Run(() => Networks.MakeDefaultNetworkAsync(second.NetworkId, TestContext.Current.CancellationToken)));
        _factory.Clock.ReadBarrier = null;

        Assert.Equal([NetworkOutcome.Succeeded, NetworkOutcome.StaleVersion], results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task ParallelDefaultChangesLeaveExactlyOneDefault()
    {
        var first = await CreateAsync("rim", "Outer Rim");
        var second = await CreateAsync("core", "Core");
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;

        await Task.WhenAll(
            Task.Run(() => Networks.MakeDefaultNetworkAsync(first.NetworkId, TestContext.Current.CancellationToken)),
            Task.Run(() => Networks.MakeDefaultNetworkAsync(second.NetworkId, TestContext.Current.CancellationToken)));
        _factory.Clock.ReadBarrier = null;

        Assert.Single(await DefaultFlagsAsync(), flag => flag.EndsWith(":True", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ParallelCreatesWithTheSameCodeLetOnlyOneSucceed()
    {
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Networks.CreateNetworkAsync("rim", Definition("Outer Rim"), TestContext.Current.CancellationToken)),
            Task.Run(() => Networks.CreateNetworkAsync("rim", Definition("Inner Rim"), TestContext.Current.CancellationToken)));
        _factory.Clock.ReadBarrier = null;

        Assert.Equal([NetworkOutcome.Succeeded, NetworkOutcome.CodeAlreadyUsed], results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task ParallelCreatesWithTheSameNameLetOnlyOneSucceed()
    {
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;

        var results = await Task.WhenAll(
            Task.Run(() => Networks.CreateNetworkAsync("rim", Definition("Outer Rim"), TestContext.Current.CancellationToken)),
            Task.Run(() => Networks.CreateNetworkAsync("rim-2", Definition("Outer Rim"), TestContext.Current.CancellationToken)));
        _factory.Clock.ReadBarrier = null;

        Assert.Equal([NetworkOutcome.Succeeded, NetworkOutcome.NameAlreadyUsed], results.Select(result => result.Outcome).Order());
    }

    [Fact]
    public async Task DisablingWhileMakingTheSameNetworkDefaultNeverLeavesADisabledDefault()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;

        await Task.WhenAll(
            Task.Run(() => Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken)),
            Task.Run(() => Networks.DisableNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken)));
        _factory.Clock.ReadBarrier = null;

        var saved = await Networks.GetNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);
        Assert.False(saved!.IsDefault && !saved.IsEnabled);
    }

    [Fact]
    public async Task DuplicateCodeIsRejectedByItsOwnUniqueIndex()
    {
        await CreateAsync("rim", "Outer Rim");

        var exception = await Record.ExceptionAsync(() => AppendCreatedAsync("rim", "Inner Rim"));

        Assert.IsType<DocumentAlreadyExistsException>(exception);
    }

    [Fact]
    public async Task DuplicateNameIsRejectedByItsOwnUniqueIndex()
    {
        await CreateAsync("rim", "Outer Rim");

        var exception = await Record.ExceptionAsync(() => AppendCreatedAsync("rim-2", "Outer Rim"));

        Assert.IsType<DocumentAlreadyExistsException>(exception);
    }

    [Fact]
    public async Task NetworkChangesAreRecordedAsEvents()
    {
        var network = await CreateAsync("rim", "Outer Rim");
        await Networks.UpdateNetworkAsync(network.NetworkId, network.Version, Definition("Far Rim"), TestContext.Current.CancellationToken);
        await Networks.MakeDefaultNetworkAsync(network.NetworkId, TestContext.Current.CancellationToken);

        await using var query = _factory.Services.GetRequiredService<IDocumentStore>().QuerySession();
        var events = await query.Events.FetchStreamAsync(network.NetworkId, token: TestContext.Current.CancellationToken);

        Assert.Equal(
            [typeof(NetworkCreated), typeof(NetworkDetailsChanged), typeof(NetworkMadeDefault)],
            events.Select(item => item.EventType));
    }

    private async Task<string[]> DefaultFlagsAsync() =>
        (await Networks.GetNetworksAsync(includeDisabled: true, TestContext.Current.CancellationToken))
        .Select(network => $"{network.Code}:{network.IsDefault}")
        .ToArray();

    private async Task AppendCreatedAsync(string code, string name)
    {
        var networkId = Guid.NewGuid();
        await using var session = _factory.Services.GetRequiredService<IDocumentStore>().LightweightSession();
        session.Events.StartStream<NetworkDocument>(
            networkId,
            new NetworkCreated(networkId, code, name, CatalogNames.Normalize(name), null, NetworkTheme.Blue, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static NetworkDefinition Definition(string name) => new(name, null, NetworkTheme.Blue);

    private async Task<NetworkSummary> CreateAsync(string code, string name) =>
        (await Networks.CreateNetworkAsync(code, Definition(name), TestContext.Current.CancellationToken)).Value!;
}
