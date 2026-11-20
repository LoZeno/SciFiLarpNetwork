namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

/// <summary>
/// Typst ran but could not make the PDF from the template: it does not compile, uses a package, reads a file that
/// is not the card data, or runs too long. The message is Typst's error, with the server's folders removed.
/// </summary>
public sealed class CardTemplateException : CardRenderingException
{
    public CardTemplateException()
    {
    }

    public CardTemplateException(string message)
        : base(message)
    {
    }

    public CardTemplateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
