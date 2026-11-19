using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure.CharacterCards;

public sealed class CardPrintingTests
{
    [Fact]
    public async Task MissingTypstExecutableIsARenderingFailure()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "typst");
        using var provider = CreateProvider(missing);
        var renderer = provider.GetRequiredService<ICharacterCardRenderer>();

        await Assert.ThrowsAsync<CardRenderingException>(() => renderer.RenderAsync(
            new CharacterCardDocument("en", new Dictionary<string, string>(), []),
            TestContext.Current.CancellationToken));
    }

    private static ServiceProvider CreateProvider(string typstPath)
    {
        var configuration = new ConfigurationManager { ["CardPrinting:TypstPath"] = typstPath };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCardPrinting(configuration);
        return services.BuildServiceProvider();
    }
}
