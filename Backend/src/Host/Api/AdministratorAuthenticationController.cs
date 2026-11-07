using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api;

[ApiController]
[Route("api/v1/admin")]
[Produces(SirenMediaTypes.Json)]
public sealed class AdministratorAuthenticationController(
    IAdministratorAuthenticationService authenticationService,
    IApplicationText text) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("session")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<SirenEntity<object>>> SignIn(
        [FromBody] AdministratorSignInRequest request,
        CancellationToken cancellationToken)
    {
        RequireSecureTransport();
        var session = await authenticationService.SignInAsync(request.UserName, request.Password, cancellationToken);
        if (session is null)
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ApiErrorCodes.Unauthorized,
                ApiErrorCodes.Unauthorized);
        }

        WriteSessionHeader(session.SessionToken);
        return Ok(CreateSessionResource(session));
    }

    [Authorize]
    [HttpGet("session")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<SirenEntity<object>>> GetCurrentSession(CancellationToken cancellationToken)
    {
        var session = await GetCurrentSessionAsync(cancellationToken);
        return Ok(CreateSessionResource(session));
    }

    [Authorize]
    [HttpPost("session/renew")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<SirenEntity<object>>> RenewSession(CancellationToken cancellationToken)
    {
        RequireSecureTransport();
        var oldToken = GetBearerToken();
        var session = await authenticationService.RenewSessionAsync(oldToken, cancellationToken);
        if (session is null)
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ApiErrorCodes.Unauthorized,
                ApiErrorCodes.Unauthorized);
        }

        WriteSessionHeader(session.SessionToken);
        return Ok(CreateSessionResource(session));
    }

    [Authorize]
    [HttpDelete("session")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await authenticationService.RevokeSessionAsync(GetBearerToken(), cancellationToken);
        return NoContent();
    }

    private async Task<AdministratorSession> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        var session = await authenticationService.ValidateSessionAsync(GetBearerToken(), cancellationToken);
        return session ?? throw new ApiException(
            StatusCodes.Status401Unauthorized,
            ApiErrorCodes.Unauthorized,
            ApiErrorCodes.Unauthorized);
    }

    private SirenEntity<object> CreateSessionResource(AdministratorSession session) => new()
    {
        Class = ["administrator-session"],
        Properties = new
        {
            accountId = session.AccountId,
            userName = session.UserName,
            role = session.Role,
            expiresAtUtc = session.ExpiresAtUtc
        },
        Links =
        [
            new SirenLink { Rel = ["self"], Href = ApiPath("/api/v1/admin/session") },
            new SirenLink { Rel = ["up"], Href = ApiPath("/api/v1") }
        ],
        Actions =
        [
            new SirenAction
            {
                Name = "administrator-renew-session",
                Href = ApiPath("/api/v1/admin/session/renew"),
                Method = "POST",
                Type = "application/json",
                Title = text.GetLocalizedText("api.renewSession")
            },
            new SirenAction
            {
                Name = "administrator-sign-out",
                Href = ApiPath("/api/v1/admin/session"),
                Method = "DELETE",
                Type = "application/json",
                Title = text.GetLocalizedText("management.signOut")
            }
        ]
    };


    private void RequireSecureTransport()
    {
        var isLoopback = HttpContext.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
        if (!Request.IsHttps && !isLoopback)
        {
            throw new ApiException(
                StatusCodes.Status403Forbidden,
                ApiErrorCodes.Forbidden,
                ApiErrorCodes.Forbidden);
        }
    }

    private string GetBearerToken() =>
        AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var header) &&
        string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(header.Parameter)
            ? header.Parameter
            : throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ApiErrorCodes.Unauthorized,
                ApiErrorCodes.Unauthorized);

    private void WriteSessionHeader(string token)
    {
        Response.Headers.Authorization = $"Bearer {token}";
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }

    private string ApiPath(string path) => $"{Request.PathBase}{path}";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdministratorSignInRequest(
    [Required, StringLength(64, MinimumLength = 3)] string UserName,
    [Required, StringLength(128, MinimumLength = 12)] string Password);
