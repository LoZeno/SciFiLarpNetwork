using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Application.Items;

/// <summary>Turns item cards into a printable PDF with the default item-card template.</summary>
public interface IItemCardRenderer
{
    /// <returns>The PDF bytes.</returns>
    /// <exception cref="CardRenderingException">Typst could not make the PDF.</exception>
    Task<byte[]> RenderAsync(ItemCardDocument document, CancellationToken cancellationToken = default);
}
