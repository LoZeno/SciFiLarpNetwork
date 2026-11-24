namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>The Markdown changes the story editor's toolbar makes to the selected text.</summary>
public static class NewsBodyEdits
{
    /// <summary>
    /// Puts <paramref name="marker"/> on both sides of the selection, such as <c>**</c> for bold; with nothing selected
    /// the caret ends up between the two markers, ready for typing.
    /// </summary>
    public static NewsBodyEdit Wrap(string text, int selectionStart, int selectionEnd, string marker)
    {
        var (start, end) = Order(text, selectionStart, selectionEnd);
        var wrapped = $"{text[..start]}{marker}{text[start..end]}{marker}{text[end..]}";
        return new NewsBodyEdit(wrapped, start + marker.Length, end + marker.Length);
    }

    /// <summary>
    /// Starts every line the selection touches with the prefix for its position, counting from 1, such as <c>- </c>
    /// for a bulleted list or <c>1. </c> for a numbered one.
    /// </summary>
    public static NewsBodyEdit PrefixLines(string text, int selectionStart, int selectionEnd, Func<int, string> prefix)
    {
        var (start, end) = Order(text, selectionStart, selectionEnd);
        var firstLine = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
        var lastLineEnd = text.IndexOf('\n', end) is var after && after >= 0 ? after : text.Length;
        var lines = text[firstLine..lastLineEnd].Split('\n');
        var prefixed = string.Join('\n', lines.Select((line, index) => prefix(index + 1) + line));
        var changed = $"{text[..firstLine]}{prefixed}{text[lastLineEnd..]}";
        return new NewsBodyEdit(changed, firstLine, firstLine + prefixed.Length);
    }

    /// <summary>Replaces the selection with <paramref name="block"/> as a paragraph of its own, and puts the caret after it.</summary>
    public static NewsBodyEdit InsertBlock(string text, int selectionStart, int selectionEnd, string block)
    {
        var (start, end) = Order(text, selectionStart, selectionEnd);
        var before = text[..start].TrimEnd(' ', '\t');
        var after = text[end..].TrimStart(' ', '\t');
        var lead = before.Length == 0 ? string.Empty : before.EndsWith("\n\n", StringComparison.Ordinal) ? string.Empty : before.EndsWith('\n') ? "\n" : "\n\n";
        var trail = after.Length == 0 ? string.Empty : after.StartsWith("\n\n", StringComparison.Ordinal) ? string.Empty : after.StartsWith('\n') ? "\n" : "\n\n";
        var inserted = before + lead + block + trail;
        return new NewsBodyEdit(inserted + after, inserted.Length, inserted.Length);
    }

    private static (int Start, int End) Order(string text, int selectionStart, int selectionEnd)
    {
        var start = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(selectionStart, selectionEnd), 0, text.Length);
        return (start, end);
    }
}
