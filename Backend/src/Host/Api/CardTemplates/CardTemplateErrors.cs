using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.CardTemplates;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <summary>Maps card template outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class CardTemplateErrors
{
    public const string InvalidCardTemplate = "errors.invalidCardTemplate";
    public const string CardTemplateTooLarge = "errors.cardTemplateTooLarge";
    public const string CardTemplateNotSaved = "errors.cardTemplateNotSaved";

    /// <summary>The error detail holding Typst's error for an invalid template.</summary>
    public const string TemplateErrorKey = "template";

    public static ApiException ToApiException(CardTemplateOutcome outcome, string? templateError = null) =>
        outcome switch
        {
            CardTemplateOutcome.InvalidTemplate => new ApiException(
                StatusCodes.Status400BadRequest,
                InvalidCardTemplate,
                InvalidCardTemplate,
                new Dictionary<string, IReadOnlyList<string>> { [TemplateErrorKey] = [templateError ?? string.Empty] }),
            CardTemplateOutcome.TemplateTooLarge => Create(StatusCodes.Status400BadRequest, CardTemplateTooLarge),
            CardTemplateOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, CatalogErrors.StaleVersion),
            CardTemplateOutcome.PersistenceFailed => Create(StatusCodes.Status503ServiceUnavailable, CardTemplateNotSaved),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static CardTemplateSummary Unwrap(CardTemplateResult result) =>
        result.Outcome == CardTemplateOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome, result.TemplateError);

    public static byte[] Unwrap(CardTemplatePreview preview) =>
        preview.Outcome == CardTemplateOutcome.Succeeded && preview.Pdf is { } pdf
            ? pdf
            : throw ToApiException(preview.Outcome, preview.TemplateError);

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
