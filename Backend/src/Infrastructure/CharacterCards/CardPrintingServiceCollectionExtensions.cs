using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Infrastructure.Items.ItemCards;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

public static class CardPrintingServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Typst card renderers, configured from the <c>CardPrinting</c> section.</summary>
        public IServiceCollection AddCardPrinting(IConfiguration configuration)
        {
            services.Configure<CardPrintingOptions>(configuration.GetSection(CardPrintingOptions.SectionName));
            services.AddSingleton<TypstRenderer>();
            services.AddSingleton<ICharacterCardRenderer, TypstCharacterCardRenderer>();
            services.AddSingleton<IItemCardRenderer, TypstItemCardRenderer>();
            return services;
        }
    }
}
