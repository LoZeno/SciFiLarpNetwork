using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SciFiLarpNetwork.Backend.Application.Administration;

namespace SciFiLarpNetwork.Backend.Api;

public sealed class AdministratorSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAdministratorAuthenticationService authenticationService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AdministratorSession";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        var isLoopback = Context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
        if (!Request.IsHttps && !isLoopback)
        {
            return AuthenticateResult.Fail("Administrator sessions require a secure connection.");
        }

        var session = await authenticationService.ValidateSessionAsync(header.Parameter, Context.RequestAborted);
        if (session is null)
        {
            return AuthenticateResult.Fail("The administrator session is invalid or expired.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.AccountId.ToString("D")),
            new Claim(ClaimTypes.Name, session.UserName),
            new Claim(ClaimTypes.Role, session.Role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
