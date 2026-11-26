using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Items;
using SciFiLarpNetwork.Backend.Localization.Text;

namespace SciFiLarpNetwork.Backend.Api.Items;

/// <summary>
/// Prints item cards. Printing creates copies and a reprint duplicates valid QR codes, so both are for administrators;
/// a preview saves nothing, so readers can preview too.
/// </summary>
[ApiController]
[Route("api/v1/item-cards")]
public sealed class ItemCardsController(IItemPrintService printService, IItemCardRenderer renderer, IApplicationText text)
    : ControllerBase
{
    public const string PdfMediaType = "application/pdf";
    public const string FileName = "item-cards.pdf";

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("print")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, PdfMediaType)]
    public async Task<IActionResult> Print([FromBody] ItemPrintRequest request, CancellationToken cancellationToken) =>
        await RenderAsync(ItemErrors.Unwrap(
            await printService.PrintAsync(request.PrintId, request.ItemIds, request.CopiesPerItem, cancellationToken)), cancellationToken);

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost("reprint")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, PdfMediaType)]
    public async Task<IActionResult> Reprint([FromBody] ItemReprintRequest request, CancellationToken cancellationToken) =>
        await RenderAsync(ItemErrors.Unwrap(await printService.ReprintAsync(request.CopyIds, cancellationToken)), cancellationToken);

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpPost("preview")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, PdfMediaType)]
    public async Task<IActionResult> Preview([FromBody] ItemPreviewRequest request, CancellationToken cancellationToken) =>
        await RenderAsync(ItemErrors.Unwrap(
            await printService.PreviewAsync(request.ItemIds, request.CopiesPerItem, cancellationToken)), cancellationToken);

    /// <remarks>
    /// When rendering fails after a print, the copies already exist; sending the same print identifier again renders
    /// them without making more.
    /// </remarks>
    private async Task<IActionResult> RenderAsync(ItemCardSet cards, CancellationToken cancellationToken)
    {
        var document = new ItemCardDocument(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            ItemCardLabels.Localize(text),
            cards.Cards);
        try
        {
            return File(await renderer.RenderAsync(document, cancellationToken), PdfMediaType, FileName);
        }
        catch (CardRenderingException)
        {
            throw ItemErrors.RenderingFailed();
        }
    }
}
