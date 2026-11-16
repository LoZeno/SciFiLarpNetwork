using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.Networks;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Networks;

[ApiController]
[Route("api/v1/networks")]
[Produces(SirenMediaTypes.Json)]
public sealed class NetworksController(
    INetworkService networkService,
    INetworkBackgroundImageService imageService,
    IApplicationText text) : ControllerBase
{
    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<NetworkCollectionProperties>>> GetNetworks(
        [FromQuery] bool includeDisabled,
        CancellationToken cancellationToken) =>
        Ok(Resources.NetworkCollection(await networkService.GetNetworksAsync(includeDisabled, cancellationToken), includeDisabled));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet("{networkId:guid}")]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> GetNetwork(Guid networkId, CancellationToken cancellationToken) =>
        Ok(Resources.Network(await networkService.GetNetworkAsync(networkId, cancellationToken)
            ?? throw NetworkErrors.ToApiException(NetworkOutcome.NotFound)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> CreateNetwork(
        [FromBody] NetworkCreateRequest request,
        CancellationToken cancellationToken)
    {
        var network = NetworkErrors.Unwrap(
            await networkService.CreateNetworkAsync(request.Code, request.ToDefinition(), cancellationToken));
        return StatusCode(StatusCodes.Status201Created, Resources.Network(network));
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{networkId:guid}")]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> UpdateNetwork(
        Guid networkId,
        [FromBody] NetworkUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Network(NetworkErrors.Unwrap(
            await networkService.UpdateNetworkAsync(networkId, request.Version, request.ToDefinition(), cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{networkId:guid}/enable")]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> EnableNetwork(Guid networkId, CancellationToken cancellationToken) =>
        Ok(Resources.Network(NetworkErrors.Unwrap(await networkService.EnableNetworkAsync(networkId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{networkId:guid}/disable")]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> DisableNetwork(Guid networkId, CancellationToken cancellationToken) =>
        Ok(Resources.Network(NetworkErrors.Unwrap(await networkService.DisableNetworkAsync(networkId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("{networkId:guid}/make-default")]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> MakeDefaultNetwork(Guid networkId, CancellationToken cancellationToken) =>
        Ok(Resources.Network(NetworkErrors.Unwrap(await networkService.MakeDefaultNetworkAsync(networkId, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut("{networkId:guid}/background-image")]
    [RequestSizeLimit(NetworkLimits.MaximumImageRequestBytes)]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> SetBackgroundImage(
        Guid networkId,
        [FromBody] NetworkBackgroundImageRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Network(NetworkErrors.Unwrap(
            await imageService.SetBackgroundImageAsync(networkId, request.Version, request.Content, cancellationToken))));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpDelete("{networkId:guid}/background-image")]
    public async Task<ActionResult<SirenEntity<NetworkSummary>>> RemoveBackgroundImage(
        Guid networkId,
        [FromBody] NetworkVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Network(NetworkErrors.Unwrap(
            await imageService.RemoveBackgroundImageAsync(networkId, request.Version, cancellationToken))));

    private NetworkResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
