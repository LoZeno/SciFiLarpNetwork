using System.Globalization;
using System.Text.Json;
using SciFiLarpNetwork.Backend.Api;
using SciFiLarpNetwork.Backend.Localization;

namespace SciFiLarpNetwork.Backend.Tests.Api;

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
        var serializedAction = document.RootElement.GetProperty("actions")[0];

        Assert.Equal("submit-message", serializedAction.GetProperty("name").GetString());
        Assert.Equal("application/json", serializedAction.GetProperty("type").GetString());
        Assert.Equal("body", serializedAction.GetProperty("fields")[0].GetProperty("name").GetString());
        Assert.False(serializedAction.TryGetProperty("class", out _));
        Assert.False(document.RootElement.TryGetProperty("properties", out _));
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

        Assert.Equal(
            "urn:scifilarnetwork:rels:navigation",
            document.RootElement.GetProperty("entities")[0].GetProperty("rel")[0].GetString());
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
    public void ApplicationTextUsesEnglishFallbackAndRejectsUnknownKey()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            var text = new ApplicationText();

            Assert.Equal("Sign in to continue.", text.GetLocalizedText("errors.unauthorized"));
            Assert.Throws<KeyNotFoundException>(() => text.GetLocalizedText("missing.key"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
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
