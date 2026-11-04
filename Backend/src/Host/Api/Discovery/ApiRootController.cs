using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Discovery;

[ApiController]
[Route("api")]
[Produces(SirenMediaTypes.Json)]
public sealed class ApiRootController(IApplicationText text) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(SirenEntity<ApiRootProperties>), StatusCodes.Status200OK)]
    public ActionResult<SirenEntity<ApiRootProperties>> GetRoot()
    {
        var apiRoot = ApiPath("/api");

        return Ok(new SirenEntity<ApiRootProperties>
        {
            Class = ["api"],
            Properties = new ApiRootProperties(text.GetLocalizedText("app.name")),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = apiRoot },
                new SirenLink
                {
                    Rel = [SirenRelations.LatestVersion],
                    Href = ApiPath("/api/v1"),
                    Title = text.GetLocalizedText("api.latestVersion")
                },
                new SirenLink
                {
                    Rel = ["service-desc"],
                    Href = ApiPath("/openapi/v1.json"),
                    Type = "application/json",
                    Title = text.GetLocalizedText("api.serviceDescription")
                }
            ]
        });
    }

    [AllowAnonymous]
    [HttpGet("v1")]
    [ProducesResponseType(typeof(SirenEntity<ApiVersionProperties>), StatusCodes.Status200OK)]
    public ActionResult<SirenEntity<ApiVersionProperties>> GetVersionOne()
    {
        var versionPath = ApiPath("/api/v1");

        return Ok(new SirenEntity<ApiVersionProperties>
        {
            Class = ["api", "version"],
            Properties = new ApiVersionProperties("v1", text.GetLocalizedText("app.name")),
            Links =
            [
                new SirenLink { Rel = ["self"], Href = versionPath },
                new SirenLink { Rel = ["up"], Href = ApiPath("/api") },
                new SirenLink
                {
                    Rel = ["service-desc"],
                    Href = ApiPath("/openapi/v1.json"),
                    Type = "application/json",
                    Title = text.GetLocalizedText("api.serviceDescription")
                }
            ]
        });
    }

    private string ApiPath(string path) => $"{Request.PathBase}{path}";
}
