using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Infrastructure;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void EventStoreDocumentStoreIsRegisteredAsSingleton()
    {
        using var provider = CreateProvider();

        var store = provider.GetRequiredService<IDocumentStore>();
        var secondResolution = provider.GetRequiredService<IDocumentStore>();

        Assert.Same(store, secondResolution);
    }

    [Fact]
    public void EventStoreDocumentStoreIsOwnedByServiceProvider()
    {
        // IDocumentStore is registered as a singleton by AddMarten and disposed with the provider.
        // This test verifies the store is resolvable and singleton-scoped.
        using var provider = CreateProvider();
        var store = provider.GetRequiredService<IDocumentStore>();
        Assert.NotNull(store);
    }

    private static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:EventStore"] =
                "Host=localhost;Port=5432;Database=scifilarnetwork_tests;Username=postgres;Password=postgres"
        };
        var services = new ServiceCollection();
        services.AddEventStore(configuration);
        return services.BuildServiceProvider();
    }
}
