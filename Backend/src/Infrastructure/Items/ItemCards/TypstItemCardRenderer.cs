using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

namespace SciFiLarpNetwork.Backend.Infrastructure.Items.ItemCards;

/// <summary>Renders item cards with the default item-card template embedded in this assembly.</summary>
internal sealed class TypstItemCardRenderer(TypstRenderer typst) : IItemCardRenderer
{
    private const string ResourceName = "SciFiLarpNetwork.Backend.Infrastructure.Items.ItemCards.item-cards.typ";

    private static readonly Lazy<byte[]> DefaultTemplate = new(ReadDefaultTemplate);

    public Task<byte[]> RenderAsync(ItemCardDocument document, CancellationToken cancellationToken = default) =>
        typst.RenderAsync(
            new TemplateItemDocument(
                document.Language,
                document.Labels,
                [.. document.Cards.Select(card => TemplateItemCard.From(card, TypstRenderer.QrSvg(card.QrCode)))]),
            DefaultTemplate.Value,
            cancellationToken);

    private static byte[] ReadDefaultTemplate()
    {
        using var resource = typeof(TypstItemCardRenderer).Assembly.GetManifestResourceStream(ResourceName)
                             ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");
        using var content = new MemoryStream();
        resource.CopyTo(content);
        return content.ToArray();
    }
}
