using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Tests.Integration.Api.ContractProbes;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api.Fixtures;

// xUnit creates this fixture through IClassFixture<ApiApplicationFactory>.
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ApiApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("PathBase", "/local");
        builder.UseSetting(
            "ConnectionStrings:EventStore",
            Environment.GetEnvironmentVariable("ConnectionStrings__EventStore") ??
            "Host=localhost;Port=5432;Database=scifilarnetwork_tests;Username=postgres;Password=postgres");
        builder.ConfigureTestServices(services =>
        {
            services
                .AddControllers()
                .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(ApiContractProbeController).Assembly));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "ContractAdmin",
                    policy => policy.RequireClaim(ClaimTypes.Role, "admin"));
            });
        });
    }

}
