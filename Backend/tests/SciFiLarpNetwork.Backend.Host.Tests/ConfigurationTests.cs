using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class ConfigurationTests
{
    [Fact]
    public void DevelopmentLoadsLocalEventStoreConnectionString()
    {
        var configuration = CreateConfiguration("Development", includeEnvironmentVariables: false);

        Assert.Equal(
            "Host=localhost;Port=5432;Database=scifilarnetwork_dev;Username=postgres;Password=postgres",
            configuration.GetConnectionString("EventStore"));
    }

    [Fact]
    public void ProductionDoesNotInheritDevelopmentConnectionString()
    {
        var configuration = CreateConfiguration("Production", includeEnvironmentVariables: false);

        Assert.Equal(string.Empty, configuration.GetConnectionString("EventStore"));
    }

    [Fact]
    public void EnvironmentVariableOverridesSettingsFile()
    {
        const string variableName = "ConnectionStrings__EventStore";
        const string overrideConnectionString = "Host=database.example;Database=production;Username=service;Password=provided-at-deploy-time";
        var originalValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, overrideConnectionString);

            var configuration = CreateConfiguration("Production");

            Assert.Equal(overrideConnectionString, configuration.GetConnectionString("EventStore"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }

    private static ConfigurationManager CreateConfiguration(
        string environmentName,
        bool includeEnvironmentVariables = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = environmentName
        });

        if (!includeEnvironmentVariables)
        {
            foreach (var source in builder.Configuration.Sources
                         .OfType<EnvironmentVariablesConfigurationSource>()
                         .ToArray())
            {
                builder.Configuration.Sources.Remove(source);
            }
        }

        return builder.Configuration;
    }
}