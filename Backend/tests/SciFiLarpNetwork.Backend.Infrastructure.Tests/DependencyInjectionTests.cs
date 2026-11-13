using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public void DefaultEventActorContextIsSystemOriginated()
    {
        using var provider = CreateProvider();

        var actorContext = provider.GetRequiredService<IEventActorContext>();

        Assert.Equal(
            new { RequiresAuthenticatedActor = false, ActorId = (string?)null },
            new { actorContext.RequiresAuthenticatedActor, actorContext.ActorId });
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
