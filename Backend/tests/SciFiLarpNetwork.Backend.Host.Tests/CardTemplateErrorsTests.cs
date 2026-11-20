using SciFiLarpNetwork.Backend.Api.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CardTemplates;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class CardTemplateErrorsTests
{
    [Theory]
    [InlineData(CardTemplateOutcome.InvalidTemplate, 400, "errors.invalidCardTemplate")]
    [InlineData(CardTemplateOutcome.EmptyTemplate, 400, "errors.invalidCardTemplate")]
    [InlineData(CardTemplateOutcome.TemplateTooLarge, 400, "errors.cardTemplateTooLarge")]
    [InlineData(CardTemplateOutcome.StaleVersion, 409, "errors.staleVersion")]
    [InlineData(CardTemplateOutcome.PersistenceFailed, 503, "errors.cardTemplateNotSaved")]
    public void OutcomeHasItsStableCode(CardTemplateOutcome outcome, int statusCode, string code)
    {
        var exception = CardTemplateErrors.ToApiException(outcome, "Typst error");

        Assert.Equal(new { StatusCode = statusCode, Code = code }, new { exception.StatusCode, exception.Code });
    }

    [Fact]
    public void InvalidTemplateCarriesTheTypstError()
    {
        var exception = CardTemplateErrors.ToApiException(CardTemplateOutcome.InvalidTemplate, "error: file not found");

        Assert.Equal(["error: file not found"], exception.Errors![CardTemplateErrors.TemplateErrorKey]);
    }

    [Fact]
    public void SuccessIsNotAnError() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CardTemplateErrors.ToApiException(CardTemplateOutcome.Succeeded));
}
