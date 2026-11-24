using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

public sealed class NewsBodyEditsTests
{
    [Fact]
    public void WrapPutsTheMarkerAroundTheSelection() =>
        Assert.Equal(new NewsBodyEdit("a **bc** d", 4, 6), NewsBodyEdits.Wrap("a bc d", 2, 4, "**"));

    [Fact]
    public void WrapWithNothingSelectedPutsTheCaretBetweenTheMarkers() =>
        Assert.Equal(new NewsBodyEdit("ab**c", 3, 3), NewsBodyEdits.Wrap("abc", 2, 2, "*"));

    [Fact]
    public void WrapAcceptsASelectionMadeBackwards() =>
        Assert.Equal("a *bc* d", NewsBodyEdits.Wrap("a bc d", 4, 2, "*").Text);

    [Fact]
    public void PrefixLinesStartsEveryTouchedLine() =>
        Assert.Equal("intro\n- one\n- two\nend", NewsBodyEdits.PrefixLines("intro\none\ntwo\nend", 7, 11, _ => "- ").Text);

    [Fact]
    public void PrefixLinesNumbersTheLines() =>
        Assert.Equal("1. one\n2. two", NewsBodyEdits.PrefixLines("one\ntwo", 0, 7, number => $"{number}. ").Text);

    [Fact]
    public void PrefixLinesWithACaretChangesOnlyItsLine() =>
        Assert.Equal("one\n## two", NewsBodyEdits.PrefixLines("one\ntwo", 5, 5, _ => "## ").Text);

    [Fact]
    public void InsertBlockSeparatesTheBlockFromTheTextAround() =>
        Assert.Equal(new NewsBodyEdit("Before.\n\n![x](media:1)\n\nAfter.", 24, 24), NewsBodyEdits.InsertBlock("Before. After.", 8, 8, "![x](media:1)"));

    [Fact]
    public void InsertBlockIntoAnEmptyBodyAddsOnlyTheBlock() =>
        Assert.Equal("![x](media:1)", NewsBodyEdits.InsertBlock(string.Empty, 0, 0, "![x](media:1)").Text);

    [Fact]
    public void InsertBlockReplacesTheSelection() =>
        Assert.Equal("A\n\n![x](media:1)\n\nC", NewsBodyEdits.InsertBlock("A\n\nB\n\nC", 3, 4, "![x](media:1)").Text);
}
