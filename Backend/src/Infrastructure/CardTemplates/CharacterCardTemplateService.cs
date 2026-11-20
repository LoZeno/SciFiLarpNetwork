using JasperFx;
using JasperFx.Events;
using Marten;
using Marten.Exceptions;
using Npgsql;
using SciFiLarpNetwork.Backend.Application.CardTemplates;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Domain.Events.CardTemplateEvents;
using SciFiLarpNetwork.Backend.Infrastructure.Administration.AdministratorDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.CardTemplates.CardTemplateDocuments;
using SciFiLarpNetwork.Backend.Infrastructure.Persistence.EventStore;

namespace SciFiLarpNetwork.Backend.Infrastructure.CardTemplates;

/// <summary>
/// Template changes follow Marten's command handler workflow on the one character-template stream:
/// <c>FetchForWriting</c> with the client's expected version, then one commit holding the content document and
/// the event. An upload is checked by printing the sample before the session is opened, so a slow check holds no
/// database resources.
/// </summary>
internal sealed class CharacterCardTemplateService(
    AttributedSessionFactory sessions,
    ICharacterCardRenderer renderer,
    TimeProvider timeProvider) : ICharacterCardTemplateService
{
    private const string CardKind = "character";
    private static readonly Guid StreamId = CardTemplateDocument.CharacterStreamId;

    public async Task<CardTemplateSummary> GetTemplateAsync(CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        return await SummarizeAsync(query, await query.LoadAsync<CardTemplateDocument>(StreamId, cancellationToken),
            cancellationToken);
    }

    public async Task<byte[]> GetSourceAsync(CancellationToken cancellationToken = default)
    {
        await using var query = sessions.QuerySession();
        if (await query.LoadAsync<CardTemplateDocument>(StreamId, cancellationToken) is { ContentId: { } contentId } &&
            await query.LoadAsync<CardTemplateContentDocument>(contentId, cancellationToken) is { } content)
        {
            return content.Content;
        }

        return await DefaultCharacterCardTemplate.LoadAsync(cancellationToken);
    }

    public async Task<CardTemplateResult> UploadAsync(long expectedVersion, byte[] templateSource, CharacterCardDocument sample,
        CancellationToken cancellationToken = default)
    {
        var check = await PreviewAsync(templateSource, sample, cancellationToken);
        if (check.Outcome != CardTemplateOutcome.Succeeded)
        {
            return new CardTemplateResult(check.Outcome, null, check.TemplateError);
        }

        await using var session = sessions.OpenSession();
        if (await FetchExpectedAsync(session, expectedVersion, cancellationToken) is not { } stream)
        {
            return CardTemplateResult.Failure(CardTemplateOutcome.StaleVersion);
        }

        var contentId = Guid.NewGuid();
        session.Store(new CardTemplateContentDocument(contentId, CardKind, templateSource));
        stream.AppendOne(new CardTemplateUploaded(CardKind, contentId, templateSource.Length, timeProvider.GetUtcNow()));
        return await SaveAsync(session, cancellationToken);
    }

    public async Task<CardTemplateResult> ResetAsync(long expectedVersion, CancellationToken cancellationToken = default)
    {
        await using var session = sessions.OpenSession();
        if (await FetchExpectedAsync(session, expectedVersion, cancellationToken) is not { } stream)
        {
            return CardTemplateResult.Failure(CardTemplateOutcome.StaleVersion);
        }

        if (stream.Aggregate?.ContentId is null)
        {
            return CardTemplateResult.Success(await SummarizeAsync(session, stream.Aggregate, cancellationToken));
        }

        stream.AppendOne(new CardTemplateReset(CardKind, timeProvider.GetUtcNow()));
        return await SaveAsync(session, cancellationToken);
    }

    public async Task<CardTemplatePreview> PreviewAsync(byte[] templateSource, CharacterCardDocument sample,
        CancellationToken cancellationToken = default)
    {
        if (templateSource.Length > ICharacterCardTemplateService.MaximumByteCount)
        {
            return new CardTemplatePreview(CardTemplateOutcome.TemplateTooLarge, null, null);
        }

        try
        {
            return new CardTemplatePreview(CardTemplateOutcome.Succeeded,
                await renderer.RenderAsync(sample, templateSource, cancellationToken), null);
        }
        catch (CardTemplateException exception)
        {
            return new CardTemplatePreview(CardTemplateOutcome.InvalidTemplate, null, exception.Message);
        }
    }

    /// <returns>The stream, or <see langword="null"/> when its version is not <paramref name="expectedVersion"/>.</returns>
    /// <remarks>
    /// Version 0 is the stream before its first event, so the first upload starts the stream; two first uploads at
    /// the same time both pass this check, and the later commit is rejected when it starts the stream again.
    /// </remarks>
    private static async Task<IEventStream<CardTemplateDocument>?> FetchExpectedAsync(
        IDocumentSession session,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await session.Events.FetchForWriting<CardTemplateDocument>(StreamId, expectedVersion, cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return null;
        }
    }

    private static async Task<CardTemplateResult> SaveAsync(IDocumentSession session, CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.TrySaveChangesAsync(cancellationToken))
            {
                return CardTemplateResult.Failure(CardTemplateOutcome.StaleVersion);
            }
        }
        catch (Marten.Exceptions.ExistingStreamIdCollisionException)
        {
            return CardTemplateResult.Failure(CardTemplateOutcome.StaleVersion);
        }
        catch (Exception exception) when (exception is NpgsqlException or MartenCommandException)
        {
            return CardTemplateResult.Failure(CardTemplateOutcome.PersistenceFailed);
        }

        return CardTemplateResult.Success(await SummarizeAsync(session,
            await session.LoadAsync<CardTemplateDocument>(StreamId, cancellationToken), cancellationToken));
    }

    private static async Task<CardTemplateSummary> SummarizeAsync(
        IQuerySession query,
        CardTemplateDocument? template,
        CancellationToken cancellationToken) =>
        template is { ContentId: not null }
            ? new CardTemplateSummary(false, await UserNameAsync(query, template.UploadedBy, cancellationToken),
                template.UploadedAtUtc, template.Version)
            : new CardTemplateSummary(true, null, null, template?.Version ?? 0);

    private static async Task<string?> UserNameAsync(IQuerySession query, string? actorId, CancellationToken cancellationToken) =>
        Guid.TryParse(actorId, out var accountId) &&
        await query.LoadAsync<AdministratorAccountDocument>(accountId, cancellationToken) is { } account
            ? account.UserName
            : actorId;
}
