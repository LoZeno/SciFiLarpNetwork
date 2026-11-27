using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.TemplateData;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

internal sealed class TypstCharacterCardRenderer(TypstRenderer typst) : ICharacterCardRenderer
{
    public Task<byte[]> RenderAsync(CharacterCardDocument document, byte[] templateSource,
        CancellationToken cancellationToken = default) =>
        typst.RenderAsync(
            new TemplateDocument(
                document.Language,
                document.Labels,
                [.. document.Cards.Select(card => TemplateCard.From(card, TypstRenderer.QrSvg(card.QrIdentifier)))]),
            templateSource,
            cancellationToken);
}
