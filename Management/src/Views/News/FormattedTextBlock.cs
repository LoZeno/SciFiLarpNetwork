using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using SciFiLarpNetwork.Management.ViewModels.News.Preview;

namespace SciFiLarpNetwork.Management.Views.News;

/// <summary>A text block that draws story text runs with their bold, italic, and code emphasis.</summary>
public sealed class FormattedTextBlock : SelectableTextBlock
{
    public static readonly StyledProperty<IReadOnlyList<NewsPreviewInline>?> RunsProperty =
        AvaloniaProperty.Register<FormattedTextBlock, IReadOnlyList<NewsPreviewInline>?>(nameof(Runs));

    private static readonly FontFamily CodeFont = new("Consolas, Menlo, monospace");

    static FormattedTextBlock()
    {
        RunsProperty.Changed.AddClassHandler<FormattedTextBlock>((block, _) => block.Rebuild());
    }

    public IReadOnlyList<NewsPreviewInline>? Runs
    {
        get => GetValue(RunsProperty);
        set => SetValue(RunsProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    private void Rebuild()
    {
        var inlines = new InlineCollection();
        foreach (var run in Runs ?? [])
        {
            var text = new Run(run.Text);
            if (run.IsBold)
            {
                text.FontWeight = FontWeight.Bold;
            }

            if (run.IsItalic)
            {
                text.FontStyle = FontStyle.Italic;
            }

            if (run.IsCode)
            {
                text.FontFamily = CodeFont;
            }

            inlines.Add(text);
        }

        Inlines = inlines;
    }
}
