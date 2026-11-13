using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
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
        Response.Headers.CacheControl = "no-store";
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = versionPath },
            new() { Rel = ["up"], Href = ApiPath("/api") },
            new()
            {
                Rel = ["service-desc"],
                Href = ApiPath("/openapi/v1.json"),
                Type = "application/json",
                Title = text.GetLocalizedText("api.serviceDescription")
            }
        };
        if (User.IsInRole(AdministratorRoles.Reader) || User.IsInRole(AdministratorRoles.Administrator))
        {
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Navigation],
                Href = ApiPath("/api/v1/admin/accounts"),
                Title = text.GetLocalizedText("api.administratorAccounts")
            });
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Navigation, SirenRelations.Talents],
                Href = ApiPath("/api/v1/talents"),
                Title = text.GetLocalizedText("api.talents")
            });
        }

        return Ok(new SirenEntity<ApiVersionProperties>
        {
            Class = ["api", "version"],
            Properties = new ApiVersionProperties("v1", text.GetLocalizedText("app.name")),
            Links = links,
            Actions = User.Identity?.IsAuthenticated == true
                ? null
                :
                [
                    new SirenAction
                    {
                        Name = "administrator-sign-in",
                        Href = ApiPath("/api/v1/admin/session"),
                        Method = "POST",
                        Type = "application/json",
                        Title = text.GetLocalizedText("management.signIn"),
                        Fields =
                        [
                            new SirenField { Name = "userName", Title = text.GetLocalizedText("management.userName") },
                            new SirenField { Name = "password", Type = "password", Title = text.GetLocalizedText("management.password") }
                        ]
                    }
                ]
        });
    }

    private string ApiPath(string path) => $"{Request.PathBase}{path}";
}
