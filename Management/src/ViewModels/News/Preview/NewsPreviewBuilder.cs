using System.Globalization;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SciFiLarpNetwork.Common.NewsMarkdown;

namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>
/// Turns a story body into the blocks the preview draws, parsed with the same Markdown dialect the backend checks.
/// Raw HTML stays text and links stay text, as players will see them.
/// </summary>
public sealed class NewsPreviewBuilder(Func<Guid, NewsMediaItem?> findMedia, Func<string, string> translate)
{
    public IReadOnlyList<NewsPreviewBlock> Build(string body)
    {
        var blocks = new List<NewsPreviewBlock>();
        AddBlocks(blocks, NewsMarkdown.Parse(body), indent: 0);
        return blocks;
    }

    private void AddBlocks(List<NewsPreviewBlock> blocks, ContainerBlock container, int indent)
    {
        foreach (var block in container)
        {
            AddBlock(blocks, block, indent, marker: string.Empty);
        }
    }

    private void AddBlock(List<NewsPreviewBlock> blocks, Block block, int indent, string marker)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AddInlines(blocks, heading.Inline, heading.Level, marker, indent);
                break;
            case ParagraphBlock paragraph:
                AddInlines(blocks, paragraph.Inline, 0, marker, indent);
                break;
            case ListBlock list:
                AddList(blocks, list, indent);
                break;
            case QuoteBlock quote:
                AddBlocks(blocks, quote, indent + 1);
                break;
            case CodeBlock code:
                blocks.Add(new NewsPreviewText([new NewsPreviewInline(code.Lines.ToString(), IsCode: true)], 0, marker, indent));
                break;
            case ThematicBreakBlock:
                blocks.Add(new NewsPreviewRule(indent));
                break;
            case ContainerBlock other:
                AddBlocks(blocks, other, indent);
                break;
        }
    }

    private void AddList(List<NewsPreviewBlock> blocks, ListBlock list, int indent)
    {
        var number = int.TryParse(list.OrderedStart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start) ? start : 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var marker = list.IsOrdered ? $"{number++}." : "•";
            var first = true;
            foreach (var child in item)
            {
                AddBlock(blocks, child, indent + 1, first ? marker : string.Empty);
                first = false;
            }
        }
    }

    /// <summary>Adds the text of a paragraph or heading, with each picture or video as a block of its own.</summary>
    private void AddInlines(List<NewsPreviewBlock> blocks, ContainerInline? container, int headingLevel, string marker,
        int indent)
    {
        var runs = new List<NewsPreviewInline>();
        void Flush()
        {
            if (runs.Count > 0 && runs.Any(run => !string.IsNullOrWhiteSpace(run.Text)))
            {
                blocks.Add(new NewsPreviewText([.. runs], headingLevel, marker, indent));
                marker = string.Empty;
            }

            runs.Clear();
        }

        foreach (var inline in container ?? [])
        {
            if (inline is LinkInline { IsImage: true } image)
            {
                Flush();
                blocks.Add(MediaBlock(image, indent));
                continue;
            }

            AddRuns(runs, inline, isBold: false, isItalic: false);
        }

        Flush();
    }

    private static void AddRuns(List<NewsPreviewInline> runs, Inline inline, bool isBold, bool isItalic)
    {
        switch (inline)
        {
            case LiteralInline literal:
                runs.Add(new NewsPreviewInline(literal.Content.ToString(), isBold, isItalic));
                break;
            case CodeInline code:
                runs.Add(new NewsPreviewInline(code.Content, isBold, isItalic, IsCode: true));
                break;
            case LineBreakInline lineBreak:
                runs.Add(new NewsPreviewInline(lineBreak.IsHard ? "\n" : " ", isBold, isItalic));
                break;
            case HtmlEntityInline entity:
                runs.Add(new NewsPreviewInline(entity.Transcoded.ToString(), isBold, isItalic));
                break;
            case AutolinkInline autolink:
                runs.Add(new NewsPreviewInline(autolink.Url, isBold, isItalic));
                break;
            case EmphasisInline emphasis:
                foreach (var child in emphasis)
                {
                    AddRuns(runs, child, isBold || emphasis.DelimiterCount >= 2, isItalic || emphasis.DelimiterCount % 2 == 1);
                }

                break;
            case ContainerInline other:
                foreach (var child in other)
                {
                    AddRuns(runs, child, isBold, isItalic);
                }

                break;
        }
    }

    private NewsPreviewBlock MediaBlock(LinkInline image, int indent)
    {
        if (NewsBodyMedia.Read(image) is not { } embed)
        {
            return new NewsPreviewProblem(translate("errors.externalImage"), indent);
        }

        if (findMedia(embed.MediaId) is not { } media)
        {
            return new NewsPreviewProblem(translate("errors.unknownMedia"), indent);
        }

        return media.IsVideo
            ? new NewsPreviewVideo(media.MediaId, embed.Text, DescribeVideo(media, embed.Options), indent)
            : new NewsPreviewPicture(media.MediaId, embed.Text, media.PictureContent, indent);
    }

    private string DescribeVideo(NewsMediaItem media, NewsVideoOptions options)
    {
        var settings = new List<string>();
        if (options.HasFlag(NewsVideoOptions.Autoplay))
        {
            settings.Add(translate("news.autoplay"));
        }

        if (options.HasFlag(NewsVideoOptions.Loop))
        {
            settings.Add(translate("news.loop"));
        }

        if (options.HasFlag(NewsVideoOptions.Controls))
        {
            settings.Add(translate("news.controls"));
        }

        string[] parts = [media.Name, media.MediaType, NewsSizes.Format(media.ByteCount), .. settings];
        return string.Join(" · ", parts);
    }
}
