namespace SciFiLarpNetwork.Backend.Infrastructure.CardTemplates;

internal static class DefaultCharacterCardTemplate
{
    private const string ResourceName = "SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.character-cards.typ";

    public static async Task<byte[]> LoadAsync(CancellationToken cancellationToken)
    {
        await using var resource = typeof(DefaultCharacterCardTemplate).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");
        using var content = new MemoryStream();
        await resource.CopyToAsync(content, cancellationToken);
        return content.ToArray();
    }
}
