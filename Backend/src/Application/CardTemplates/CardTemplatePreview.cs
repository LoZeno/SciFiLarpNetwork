namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

/// <param name="Pdf">The sample cards printed with the template, when the outcome is <see cref="CardTemplateOutcome.Succeeded"/>.</param>
/// <param name="TemplateError">Typst's error when the outcome is <see cref="CardTemplateOutcome.InvalidTemplate"/>.</param>
public sealed record CardTemplatePreview(CardTemplateOutcome Outcome, byte[]? Pdf, string? TemplateError);
