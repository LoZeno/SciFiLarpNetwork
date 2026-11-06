using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SciFiLarpNetwork.Backend.Infrastructure;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void EventStoreDataSourceIsRegisteredAsSingleton()
    {
        using var provider = CreateProvider();

        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        var secondResolution = provider.GetRequiredService<NpgsqlDataSource>();

        Assert.Same(dataSource, secondResolution);
    }

    [Fact]
    public async Task EventStoreDataSourceIsDisposedWithServiceProvider()
    {
        var provider = CreateProvider();
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        provider.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken));
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