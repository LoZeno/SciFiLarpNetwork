using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Api;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Tests.Host;

public sealed class SirenContractTests
{
    [Fact]
    public void ActionSerializesSirenNamesAndJsonFieldMetadata()
    {
        var action = new SirenAction
        {
            Name = "submit-message",
            Href = "/api/v1/messages",
            Method = "POST",
            Title = "Submit message",
            Type = "application/json",
            Fields =
            [
                new SirenField { Name = "body", Title = "Message", Type = "text" }
            ]
        };
        var entity = new SirenEntity<object>
        {
            Class = ["message-form"],
            Actions = [action]
        };

        using var document = JsonDocument.Parse(SirenJson.Serialize(entity));
        using var expected = JsonDocument.Parse(
            """{"class":["message-form"],"actions":[{"name":"submit-message","href":"/api/v1/messages","method":"POST","title":"Submit message","type":"application/json","fields":[{"name":"body","title":"Message","type":"text"}]}]}""");

        Assert.True(JsonElement.DeepEquals(expected.RootElement, document.RootElement));
    }

    [Fact]
    public void EmbeddedSirenEntityIncludesRequiredRelationship()
    {
        var embedded = new SirenSubEntity
        {
            Rel = ["urn:scifilarnetwork:rels:navigation"],
            Class = ["navigation-item"],
            Href = "/api/v1/home"
        };
        var entity = new SirenEntity<object>
        {
            Class = ["api"],
            Entities = [embedded]
        };

        using var document = JsonDocument.Parse(SirenJson.Serialize(entity));
        using var expected = JsonDocument.Parse(
            """{"class":["api"],"entities":[{"rel":["urn:scifilarnetwork:rels:navigation"],"class":["navigation-item"],"href":"/api/v1/home"}]}""");

        Assert.True(JsonElement.DeepEquals(expected.RootElement, document.RootElement));
    }

    [Theory]
    [InlineData("errors.unauthorized", "Accedi per continuare.")]
    [InlineData("api.latestVersion", "Versione API più recente")]
    public void ApplicationTextUsesItalianForLocalizedResources(string key, string expected)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("it");

            Assert.Equal(expected, new ApplicationText().GetLocalizedText(key));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ApplicationTextUsesEnglishFallbackForUnsupportedCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");

            Assert.Equal("Sign in to continue.", new ApplicationText().GetLocalizedText("errors.unauthorized"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ApplicationTextRejectsUnknownKey()
    {
        Assert.Throws<KeyNotFoundException>(() => new ApplicationText().GetLocalizedText("missing.key"));
    }

    [Theory]
    [InlineData(400, "errors.validation")]
    [InlineData(401, "errors.unauthorized")]
    [InlineData(403, "errors.forbidden")]
    [InlineData(404, "errors.notFound")]
    [InlineData(409, "errors.conflict")]
    [InlineData(418, "errors.requestFailed")]
    [InlineData(503, "errors.serviceUnavailable")]
    [InlineData(500, "errors.internalServerError")]
    public void ErrorCodesMapHttpStatusToStableCode(int statusCode, string expectedCode)
    {
        Assert.Equal(expectedCode, ApiErrorCodes.FromStatusCode(statusCode).Code);
    }

    [Theory]
    [InlineData(399)]
    [InlineData(600)]
    public void ApiExceptionRejectsNonErrorHttpStatus(int statusCode)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApiException(statusCode, ApiErrorCodes.Conflict, ApiErrorCodes.Conflict));
    }

    [Fact]
    public void ApiExceptionRejectsMissingMachineReadableCode()
    {
        Assert.Throws<ArgumentException>(() =>
            new ApiException(409, " ", ApiErrorCodes.Conflict));
    }
}
