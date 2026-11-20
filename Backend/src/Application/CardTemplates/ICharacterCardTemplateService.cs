using SciFiLarpNetwork.Backend.Application.CharacterCards;

namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

/// <summary>Keeps the Typst template that prints character cards: the default one, or one an administrator uploaded.</summary>
public interface ICharacterCardTemplateService
{
    /// <summary>The largest template file accepted, in bytes (256 KB).</summary>
    const int MaximumByteCount = 256 * 1024;

    Task<CardTemplateSummary> GetTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>The source of the template in use: the uploaded one, or the default one.</summary>
    Task<byte[]> GetSourceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints <paramref name="sample"/> with <paramref name="templateSource"/> and saves the template only when that works.
    /// </summary>
    /// <exception cref="CardRenderingException">Typst could not be run, so the template could not be checked.</exception>
    Task<CardTemplateResult> UploadAsync(long expectedVersion, byte[] templateSource, CharacterCardDocument sample,
        CancellationToken cancellationToken = default);

    /// <summary>Goes back to the default template. Nothing changes when it is already in use.</summary>
    Task<CardTemplateResult> ResetAsync(long expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>Prints <paramref name="sample"/> with <paramref name="templateSource"/> without saving anything.</summary>
    /// <exception cref="CardRenderingException">Typst could not be run.</exception>
    Task<CardTemplatePreview> PreviewAsync(byte[] templateSource, CharacterCardDocument sample,
        CancellationToken cancellationToken = default);
}
