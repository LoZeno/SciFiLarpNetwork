using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api;

[ApiController]
[Route("api/v1/admin/accounts")]
[Produces(SirenMediaTypes.Json)]
public sealed class AdministratorAccountsController(
    IAdministratorAuthenticationService authenticationService,
    IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<AdministratorSummary>>> Create(
        [FromBody] AdministratorAccountCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorAccountId))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ApiErrorCodes.Unauthorized,
                ApiErrorCodes.Unauthorized);
        }

        var result = await authenticationService.CreateAccountAsync(
            actorAccountId,
            request.UserName,
            request.Password,
            request.Role,
            cancellationToken);
        if (result.Result != AdministratorOperationResult.Succeeded || result.Account is null)
        {
            throw result.Result switch
            {
                AdministratorOperationResult.AccountNameAlreadyUsed => new ApiException(
                    StatusCodes.Status409Conflict,
                    ApiErrorCodes.Conflict,
                    ApiErrorCodes.Conflict),
                AdministratorOperationResult.InvalidGrant => new ApiException(
                    StatusCodes.Status400BadRequest,
                    ApiErrorCodes.Validation,
                    ApiErrorCodes.Validation),
                _ => new ApiException(
                    StatusCodes.Status403Forbidden,
                    ApiErrorCodes.Forbidden,
                    ApiErrorCodes.Forbidden)
            };
        }

        return StatusCode(StatusCodes.Status201Created, CreateAccountResource(result.Account));
    }
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<AdministratorDirectoryProperties>>> Get(
        CancellationToken cancellationToken)
    {
        var accounts = await authenticationService.GetAdministratorsAsync(cancellationToken);
        return Ok(new SirenEntity<AdministratorDirectoryProperties>
        {
            Class = ["administrator-directory"],
            Properties = new AdministratorDirectoryProperties(
                text.GetLocalizedText("api.administratorAccounts"),
                text.GetLocalizedText("api.administratorAccountsDescription"),
                accounts),
            Links = [new SirenLink { Rel = ["self"], Href = ApiPath("/api/v1/admin/accounts") }],
            Actions = User.IsInRole(AdministratorRoles.Administrator)
                ?
                [
                    new SirenAction
                    {
                        Name = "create-administrator",
                        Href = ApiPath("/api/v1/admin/accounts"),
                        Method = "POST",
                        Type = "application/json",
                        Title = text.GetLocalizedText("api.createAdministrator"),
                        Fields =
                        [
                            new SirenField { Name = "userName", Title = text.GetLocalizedText("management.userName") },
                            new SirenField { Name = "password", Type = "password", Title = text.GetLocalizedText("management.password") },
                            new SirenField { Name = "role", Title = text.GetLocalizedText("api.administratorRole") }
                        ]
                    }
                ]
                : null
        });
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<SirenEntity<AdministratorSummary>>> GetAccount(
        Guid accountId,
        CancellationToken cancellationToken) =>
        Ok(CreateAccountResource(await FindAccountAsync(accountId, cancellationToken)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{accountId:guid}/role")]
    public async Task<ActionResult<SirenEntity<AdministratorSummary>>> ChangeRole(
        Guid accountId,
        [FromBody] AdministratorRoleChangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorAccountId))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ApiErrorCodes.Unauthorized,
                ApiErrorCodes.Unauthorized);
        }

        var result = await authenticationService.ChangeRoleAsync(
            actorAccountId,
            accountId,
            request.Role,
            cancellationToken);
        if (result != AdministratorOperationResult.Succeeded)
        {
            throw result switch
            {
                AdministratorOperationResult.LastAdministrator => new ApiException(
                    StatusCodes.Status409Conflict,
                    ApiErrorCodes.Conflict,
                    ApiErrorCodes.Conflict),
                AdministratorOperationResult.AccountNotFound => new ApiException(
                    StatusCodes.Status404NotFound,
                    ApiErrorCodes.NotFound,
                    ApiErrorCodes.NotFound),
                _ => new ApiException(
                    StatusCodes.Status403Forbidden,
                    ApiErrorCodes.Forbidden,
                    ApiErrorCodes.Forbidden)
            };
        }

        return Ok(CreateAccountResource(await FindAccountAsync(accountId, cancellationToken)));
    }

    private async Task<AdministratorSummary> FindAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        (await authenticationService.GetAdministratorsAsync(cancellationToken))
        .FirstOrDefault(account => account.AccountId == accountId)
        ?? throw new ApiException(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound, ApiErrorCodes.NotFound);

    private SirenEntity<AdministratorSummary> CreateAccountResource(AdministratorSummary account)
    {
        var accountPath = ApiPath($"/api/v1/admin/accounts/{account.AccountId:D}");
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var canChangeRole = User.IsInRole(AdministratorRoles.Administrator) &&
                            !(string.Equals(actorId, account.AccountId.ToString("D"), StringComparison.OrdinalIgnoreCase) &&
                              account.Role != AdministratorRoles.Administrator);
        return new SirenEntity<AdministratorSummary>
        {
            Class = ["administrator-account"],
            Properties = account,
            Links =
            [
                new SirenLink { Rel = ["self"], Href = accountPath },
                new SirenLink { Rel = ["collection"], Href = ApiPath("/api/v1/admin/accounts") }
            ],
            Actions = canChangeRole
                ?
                [
                    new SirenAction
                    {
                        Name = "change-administrator-role",
                        Href = $"{accountPath}/role",
                        Method = "PUT",
                        Type = "application/json",
                        Title = text.GetLocalizedText("api.changeAdministratorRole"),
                        Fields = [new SirenField { Name = "role", Title = text.GetLocalizedText("api.administratorRole") }]
                    }
                ]
                : null
        };
    }

    private string ApiPath(string path) => $"{Request.PathBase}{path}";
}

public sealed record AdministratorDirectoryProperties(
    string Title,
    string Description,
    IReadOnlyList<AdministratorSummary> Administrators);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdministratorRoleChangeRequest(
    [Required]
    [RegularExpression("^(reader|administrator)$")]
    string Role);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdministratorAccountCreateRequest(
    [Required]
    [StringLength(64, MinimumLength = 3)]
    string UserName,
    [Required]
    [StringLength(128, MinimumLength = 12)]
    string Password,
    [Required]
    [RegularExpression("^(reader|administrator)$")]
    string Role);
