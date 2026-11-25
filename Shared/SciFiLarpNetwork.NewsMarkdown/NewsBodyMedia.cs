using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace SciFiLarpNetwork.Common.NewsMarkdown;

/// <summary>Finds and writes the pictures and videos of a news body.</summary>
public static class NewsBodyMedia
{
    public const string Scheme = "media:";

    private static readonly Dictionary<string, NewsVideoOptions> OptionNames = new(StringComparer.Ordinal)
    {
        ["autoplay"] = NewsVideoOptions.Autoplay,
        ["loop"] = NewsVideoOptions.Loop,
        ["controls"] = NewsVideoOptions.Controls
    };

    /// <summary>Every image of the body in document order, or the first problem found.</summary>
    public static NewsBodyMediaScan Scan(string body)
    {
        var embeds = new List<NewsMediaEmbed>();
        foreach (var image in Images(NewsMarkdown.Parse(body)))
        {
            if (Read(image) is not { } embed)
            {
                return NewsBodyMediaScan.Failed(Problem(image));
            }

            if (string.IsNullOrWhiteSpace(embed.Text))
            {
                return NewsBodyMediaScan.Failed(NewsBodyMediaProblem.MissingText);
            }

            embeds.Add(embed);
        }

        return new NewsBodyMediaScan(embeds, null);
    }

    /// <summary>The images of a parsed body, in document order.</summary>
    public static IEnumerable<LinkInline> Images(MarkdownDocument document) =>
        document.Descendants<LinkInline>().Where(link => link.IsImage);

    /// <summary>The library media an image names, or <see langword="null"/> when it names anything else.</summary>
    public static NewsMediaEmbed? Read(LinkInline image)
    {
        if (image.Url is not { } url || !url.StartsWith(Scheme, StringComparison.Ordinal))
        {
            return null;
        }

        var address = url[Scheme.Length..];
        var query = address.IndexOf('?', StringComparison.Ordinal);
        var id = query < 0 ? address : address[..query];
        if (!Guid.TryParseExact(id, "D", out var mediaId) || mediaId == Guid.Empty)
        {
            return null;
        }

        var options = query < 0 ? NewsVideoOptions.None : ReadOptions(address[(query + 1)..]);
        return options is { } valid ? new NewsMediaEmbed(mediaId, Text(image).Trim(), valid) : null;
    }

    /// <summary>The Markdown that places a library picture or video, for editors to insert.</summary>
    public static string Write(NewsMediaEmbed embed)
    {
        var options = OptionNames.Where(option => embed.Options.HasFlag(option.Value)).Select(option => option.Key).ToArray();
        var address = options.Length == 0
            ? $"{Scheme}{embed.MediaId:D}"
            : $"{Scheme}{embed.MediaId:D}?{string.Join('&', options)}";
        return $"![{EscapeText(embed.Text)}]({address})";
    }

    private static NewsBodyMediaProblem Problem(LinkInline image) =>
        image.Url is { } url && url.StartsWith(Scheme, StringComparison.Ordinal)
            ? NewsBodyMediaProblem.InvalidReference
            : NewsBodyMediaProblem.ExternalImage;

    private static NewsVideoOptions? ReadOptions(string query)
    {
        var options = NewsVideoOptions.None;
        foreach (var name in query.Split('&'))
        {
            if (!OptionNames.TryGetValue(name, out var option) || options.HasFlag(option))
            {
                return null;
            }

            options |= option;
        }

        return options;
    }

    private static string Text(ContainerInline container)
    {
        var text = new StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content);
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case LineBreakInline:
                    text.Append(' ');
                    break;
                case ContainerInline child:
                    text.Append(Text(child));
                    break;
            }
        }

        return text.ToString();
    }

    private static string EscapeText(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (var character in text.ReplaceLineEndings(" "))
        {
            if (character is '\\' or '[' or ']' or '*' or '_' or '`' or '!' or '<' or '&')
            {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }

        return escaped.ToString();
    }
}
