namespace SciFiLarpNetwork.Backend.Application.CharacterCards;

public sealed class CardRenderingException : Exception
{
    public CardRenderingException()
    {
    }

    public CardRenderingException(string message)
        : base(message)
    {
    }

    public CardRenderingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
