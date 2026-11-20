using SciFiLarpNetwork.Backend.Api.Skills;
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Localization.Text;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <summary>Maps card template outcomes to stable Siren error codes. Each code is also its localization key.</summary>
public static class CardTemplateErrors
{
    public const string InvalidCardTemplate = "errors.invalidCardTemplate";
    public const string CardTemplateTooLarge = "errors.cardTemplateTooLarge";
    public const string CardTemplateNotSaved = "errors.cardTemplateNotSaved";

    /// <summary>The error detail holding Typst's error for an invalid template.</summary>
    public const string TemplateErrorKey = "template";

    /// <summary>The localization key of the <see cref="TemplateErrorKey"/> text for an empty file.</summary>
    public const string EmptyFileKey = "cardTemplates.emptyFile";

    /// <param name="templateError">
    /// The text for the <see cref="TemplateErrorKey"/> detail: Typst's error for an invalid template, or the
    /// localized empty-file message for an empty one.
    /// </param>
    public static ApiException ToApiException(CardTemplateOutcome outcome, string? templateError = null) =>
        outcome switch
        {
            CardTemplateOutcome.InvalidTemplate or CardTemplateOutcome.EmptyTemplate => new ApiException(
                StatusCodes.Status400BadRequest,
                InvalidCardTemplate,
                InvalidCardTemplate,
                new Dictionary<string, IReadOnlyList<string>> { [TemplateErrorKey] = [templateError ?? string.Empty] }),
            CardTemplateOutcome.TemplateTooLarge => Create(StatusCodes.Status400BadRequest, CardTemplateTooLarge),
            CardTemplateOutcome.StaleVersion => Create(StatusCodes.Status409Conflict, CatalogErrors.StaleVersion),
            CardTemplateOutcome.PersistenceFailed => Create(StatusCodes.Status503ServiceUnavailable, CardTemplateNotSaved),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "A successful outcome is not an error.")
        };

    public static CardTemplateSummary Unwrap(CardTemplateResult result, IApplicationText text) =>
        result.Outcome == CardTemplateOutcome.Succeeded && result.Value is { } value
            ? value
            : throw ToApiException(result.Outcome, TemplateError(result.Outcome, result.TemplateError, text));

    public static byte[] Unwrap(CardTemplatePreview preview, IApplicationText text) =>
        preview.Outcome == CardTemplateOutcome.Succeeded && preview.Pdf is { } pdf
            ? pdf
            : throw ToApiException(preview.Outcome, TemplateError(preview.Outcome, preview.TemplateError, text));

    private static string? TemplateError(CardTemplateOutcome outcome, string? typstError, IApplicationText text) =>
        outcome == CardTemplateOutcome.EmptyTemplate ? text.GetLocalizedText(EmptyFileKey) : typstError;

    private static ApiException Create(int statusCode, string code) => new(statusCode, code, code);
}
