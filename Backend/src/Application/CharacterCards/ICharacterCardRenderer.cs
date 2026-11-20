namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>Turns character cards into a printable PDF.</summary>
public interface ICharacterCardRenderer
{
    /// <param name="templateSource">The Typst template source, UTF-8 encoded.</param>
    /// <returns>The PDF bytes.</returns>
    /// <exception cref="CardTemplateException">Typst could not make the PDF from <paramref name="templateSource"/>.</exception>
    /// <exception cref="CardRenderingException">Typst could not be run.</exception>
    Task<byte[]> RenderAsync(CharacterCardDocument document, byte[] templateSource, CancellationToken cancellationToken = default);
}
