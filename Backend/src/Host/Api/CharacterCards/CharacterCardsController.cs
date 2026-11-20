using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Localization.Text;

namespace SciFiLarpNetwork.Backend.Api.CharacterCards;

/// <summary>Prints character cards. Printing changes nothing, so readers can print too.</summary>
[ApiController]
[Route("api/v1/character-cards")]
public sealed class CharacterCardsController(
    ICharacterCardService cardService,
    ICharacterCardRenderer renderer,
    IApplicationText text) : ControllerBase
{
    public const string PdfMediaType = "application/pdf";
    public const string FileName = "character-cards.pdf";

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpPost]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, PdfMediaType)]
    public async Task<IActionResult> PrintCharacterCards(
        [FromBody] CharacterCardsRequest request,
        CancellationToken cancellationToken)
    {
        var cards = CharacterCardErrors.Unwrap(await cardService.GetCardsAsync(request.CharacterIds, cancellationToken));
        var document = new CharacterCardDocument(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            CharacterCardLabels.Localize(text),
            cards);
        try
        {
            return File(await renderer.RenderAsync(document, cancellationToken), PdfMediaType, FileName);
        }
        catch (CardRenderingException)
        {
            throw CharacterCardErrors.RenderingFailed();
        }
    }
}
