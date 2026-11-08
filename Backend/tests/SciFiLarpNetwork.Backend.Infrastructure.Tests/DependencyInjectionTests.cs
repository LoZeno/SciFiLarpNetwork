using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Marten;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Infrastructure;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void DocumentStoreIsRegisteredAsSingleton()
    {
        using var provider = CreateProvider();

        var documentStore = provider.GetRequiredService<IDocumentStore>();
        var secondResolution = provider.GetRequiredService<IDocumentStore>();

        Assert.Same(documentStore, secondResolution);
    }

    [Fact]
    public void EventStreamStoreIsRegisteredAsSingleton()
    {
        using var provider = CreateProvider();

        var eventStore = provider.GetRequiredService<IEventStreamStore>();
        var secondResolution = provider.GetRequiredService<IEventStreamStore>();

        Assert.Same(eventStore, secondResolution);
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