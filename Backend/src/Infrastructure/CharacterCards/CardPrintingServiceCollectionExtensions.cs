using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

public static class CardPrintingServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Typst card renderer, configured from the <c>CardPrinting</c> section.</summary>
        public IServiceCollection AddCardPrinting(IConfiguration configuration)
        {
            services.Configure<CardPrintingOptions>(configuration.GetSection(CardPrintingOptions.SectionName));
            services.AddSingleton<ICharacterCardRenderer, TypstCharacterCardRenderer>();
            return services;
        }
    }
}
