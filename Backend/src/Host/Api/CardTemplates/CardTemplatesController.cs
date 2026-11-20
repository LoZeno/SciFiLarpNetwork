using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciFiLarpNetwork.Backend.Api.CharacterCards;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.CardTemplates;

/// <summary>
/// Shows, downloads, uploads, previews, and resets the character-card template. Readers can see and download it;
/// only administrators change or preview it.
/// </summary>
[ApiController]
[Route("api/v1/card-templates")]
[Produces(SirenMediaTypes.Json)]
public sealed class CardTemplatesController(ICharacterCardTemplateService templates, IApplicationText text) : ControllerBase
{
    public const string TemplateMediaType = "text/plain";
    public const string TemplateFileName = "character-cards.typ";
    public const string PreviewFileName = "character-cards-preview.pdf";

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet]
    public async Task<ActionResult<SirenEntity<CardTemplateCollectionProperties>>> GetCardTemplates(
        CancellationToken cancellationToken) =>
        Ok(Resources.Collection(await templates.GetTemplateAsync(cancellationToken)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet(CardTemplateResources.CharacterKind)]
    public async Task<ActionResult<SirenEntity<CardTemplateProperties>>> GetCharacterCardTemplate(
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(await templates.GetTemplateAsync(cancellationToken)));

    [Authorize(Policy = AdministratorAuthorizationPolicies.Read)]
    [HttpGet(CardTemplateResources.CharacterKind + "/source")]
    [Produces(TemplateMediaType)]
    public async Task<IActionResult> DownloadCharacterCardTemplate(CancellationToken cancellationToken) =>
        File(await templates.GetSourceAsync(cancellationToken), $"{TemplateMediaType}; charset=utf-8", TemplateFileName);

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPut(CardTemplateResources.CharacterKind)]
    [RequestSizeLimit(CardTemplateLimits.MaximumRequestBytes)]
    public async Task<ActionResult<SirenEntity<CardTemplateProperties>>> UploadCharacterCardTemplate(
        [FromBody] CardTemplateUploadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(Resources.Character(CardTemplateErrors.Unwrap(
                await templates.UploadAsync(request.Version, request.Content, Sample(), cancellationToken))));
        }
        catch (CardRenderingException)
        {
            throw CharacterCardErrors.RenderingFailed();
        }
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost(CardTemplateResources.CharacterKind + "/preview")]
    [RequestSizeLimit(CardTemplateLimits.MaximumRequestBytes)]
    [Produces(CharacterCardsController.PdfMediaType)]
    public async Task<IActionResult> PreviewCharacterCardTemplate(
        [FromBody] CardTemplatePreviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return File(CardTemplateErrors.Unwrap(await templates.PreviewAsync(request.Content, Sample(), cancellationToken)),
                CharacterCardsController.PdfMediaType, PreviewFileName);
        }
        catch (CardRenderingException)
        {
            throw CharacterCardErrors.RenderingFailed();
        }
    }

    [Authorize(Policy = AdministratorAuthorizationPolicies.Write)]
    [HttpPost(CardTemplateResources.CharacterKind + "/reset")]
    public async Task<ActionResult<SirenEntity<CardTemplateProperties>>> ResetCharacterCardTemplate(
        [FromBody] CardTemplateVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(Resources.Character(CardTemplateErrors.Unwrap(await templates.ResetAsync(request.Version, cancellationToken))));

    private CharacterCardDocument Sample() =>
        new(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, CharacterCardLabels.Localize(text), SampleCharacterCards.Cards);

    private CardTemplateResources Resources =>
        new(text, Request.PathBase, User.IsInRole(AdministratorRoles.Administrator));
}
