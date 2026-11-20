namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

/// <summary>The <c>CardPrinting</c> configuration section.</summary>
public sealed class CardPrintingOptions
{
    public const string SectionName = "CardPrinting";

    /// <summary>
    /// The Typst executable. When unset, the copy bundled with the backend under <c>tools/typst</c> is used.
    /// </summary>
    public string? TypstPath { get; set; }

    /// <summary>How long one PDF may take before rendering is stopped.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
}
