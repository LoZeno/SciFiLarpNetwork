namespace SciFiLarpNetwork.Backend.Application.CardTemplates;

/// <param name="Value">The template state after the change, when the outcome is <see cref="CardTemplateOutcome.Succeeded"/>.</param>
/// <param name="TemplateError">Typst's error when the outcome is <see cref="CardTemplateOutcome.InvalidTemplate"/>.</param>
public sealed record CardTemplateResult(CardTemplateOutcome Outcome, CardTemplateSummary? Value, string? TemplateError)
{
    public static CardTemplateResult Success(CardTemplateSummary value) => new(CardTemplateOutcome.Succeeded, value, null);

    public static CardTemplateResult Failure(CardTemplateOutcome outcome) => new(outcome, null, null);

    public static CardTemplateResult Invalid(string templateError) =>
        new(CardTemplateOutcome.InvalidTemplate, null, templateError);
}
