namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>Turns character cards into a printable PDF.</summary>
public interface ICharacterCardRenderer
{
    /// <returns>The PDF bytes.</returns>
    /// <exception cref="CardRenderingException">The PDF could not be made.</exception>
    Task<byte[]> RenderAsync(CharacterCardDocument document, CancellationToken cancellationToken = default);
}
