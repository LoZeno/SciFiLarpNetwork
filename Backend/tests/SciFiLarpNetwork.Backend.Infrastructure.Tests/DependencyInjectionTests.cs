using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SciFiLarpNetwork.Backend.Infrastructure;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task EventStoreDataSourceIsSingletonAndDisposedWithServiceProvider()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:EventStore"] =
                "Host=localhost;Port=5432;Database=scifilarnetwork_tests;Username=postgres;Password=postgres"
        };
        var services = new ServiceCollection();
        services.AddEventStore(configuration);
        var provider = services.BuildServiceProvider();

        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        Assert.Same(dataSource, provider.GetRequiredService<NpgsqlDataSource>());

        provider.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await dataSource.OpenConnectionAsync());
    }
}