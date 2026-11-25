using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Application.News;
using SciFiLarpNetwork.Backend.Application.Players;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Networks;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;
using SciFiLarpNetwork.Backend.Tests.Integration.Messages;
using SciFiLarpNetwork.Backend.Tests.Integration.Networks;
using SciFiLarpNetwork.Backend.Tests.Integration.Players;

namespace SciFiLarpNetwork.Backend.Tests.Integration.News;

/// <summary>Creates networks, characters, media, and stories through the public services.</summary>
internal sealed class NewsTestData(AdministratorAuthenticationFactory factory)
{
    private readonly MessageTestData _characters = new(factory);

    public INewsStoryService Stories => factory.Services.GetRequiredService<INewsStoryService>();

    public INewsMediaService Media => factory.Services.GetRequiredService<INewsMediaService>();

    public INetworkService Networks => factory.Services.GetRequiredService<INetworkService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async Task<NetworkSummary> CreateNetworkAsync(string code = "core", string name = "Core network") =>
        (await Networks.CreateNetworkAsync(code, new NetworkDefinition(name, null, NetworkTheme.Blue), Token)).Value!;

    public async Task<NetworkSummary> CreateDisabledNetworkAsync(string code = "old", string name = "Old network")
    {
        var network = await CreateNetworkAsync(code, name);
        return (await Networks.DisableNetworkAsync(network.NetworkId, Token)).Value!;
    }

    public Task<CharacterSummary> CreateCharacterAsync(string name = "Kira", CharacterStatus status = CharacterStatus.Active) =>
        _characters.CreateCharacterAsync(name, status);

    public Task<CharacterSummary> CreateArchivedCharacterAsync(string name = "Old Tom") =>
        _characters.CreateArchivedCharacterAsync(name);

    /// <summary>Retires and archives a character, as only a retired or dead character can be archived.</summary>
    public async Task ArchiveAsync(CharacterSummary character)
    {
        var retired = (await _characters.Characters.UpdateCharacterAsync(character.CharacterId, character.Version,
            CharacterDefinitions.From(character) with { Status = CharacterStatus.Retired }, Token)).Value!;
        await _characters.Characters.ArchiveCharacterAsync(retired.CharacterId, Token);
    }

    /// <summary>A story on <paramref name="network"/>, active from an hour ago to an hour from now.</summary>
    public NewsStoryContent Content(NetworkSummary network, CharacterSummary author, string body = "The fleet arrives **tonight**.")
    {
        var now = factory.Clock.GetUtcNow();
        return new NewsStoryContent(network.NetworkId, author.CharacterId, "Fleet arrives", body, now.AddHours(-1), now.AddHours(1), 2);
    }

    public async Task<NewsStoryDetails> CreateAsync(NewsStoryContent content) =>
        (await Stories.CreateStoryAsync(Guid.NewGuid(), content, Token)).Value!;

    public async Task<NewsMediaSummary> UploadAsync(byte[] content, string name = "Harbour at dawn")
    {
        using var stream = new MemoryStream(content);
        return (await Media.UploadMediaAsync(name, stream, Token)).Value!;
    }

    public Task<NewsMediaSummary> UploadPictureAsync(string name = "Harbour at dawn") => UploadAsync(Picture(), name);

    public Task<NewsMediaSummary> UploadVideoAsync(string name = "Launch") => UploadAsync(Video(), name);

    public static byte[] Picture(int size = 2048, int width = 640, int height = 480) => NetworkTestImages.Png(size, width, height);

    public static byte[] Video(int size = 4096) => [.. Mp4Header, .. Enumerable.Range(0, size).Select(index => (byte)(index % 251))];

    public static byte[] Mp4Header =>
        [0x00, 0x00, 0x00, 0x18, .. "ftyp"u8, .. "isom"u8, 0x00, 0x00, 0x02, 0x00, .. "isomiso2"u8];

    public static string Embed(NewsMediaSummary media, string options = "") =>
        $"![{media.Name}](media:{media.MediaId:D}{(options.Length == 0 ? string.Empty : "?" + options)})";

    public static byte[] Text(string text) => Encoding.UTF8.GetBytes(text);
}
