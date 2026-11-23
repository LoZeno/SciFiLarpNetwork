using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Messages;
using SciFiLarpNetwork.Backend.Domain.Characters;
using SciFiLarpNetwork.Backend.Domain.Events.MessageEvents;
using SciFiLarpNetwork.Backend.Tests.Integration.Api;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Messages;

[Collection("Backend database integration")]
public sealed class MessageServiceTests(AdministratorAuthenticationFactory factory) : IAsyncLifetime
{
    private readonly MessageTestData _data = new(factory);

    private IMessageService Messages => _data.Messages;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SentMessageHasItsSenderRecipientsAndUntouchedBody()
    {
        var sender = await _data.CreateCharacterAsync("Kira");
        var recipient = await _data.CreateCharacterAsync("Jon");
        var draft = MessageTestData.Draft(sender, recipient) with { Title = "  Supply run ", Body = "  Meet at the dock.\r\n\r\n— K.  " };

        var result = await Messages.SendMessageAsync(draft, Token);

        Assert.Equivalent(
            new MessageDetails(
                draft.MessageId,
                new MessageCharacter(sender.CharacterId, true, sender.Number, "Kira", false),
                [new MessageCharacter(recipient.CharacterId, true, recipient.Number, "Jon", false)],
                "Supply run",
                "  Meet at the dock.\r\n\r\n— K.  ",
                result.Value!.SentAtUtc,
                null,
                null),
            result.Value,
            strict: true);
    }

    [Fact]
    public async Task SentMessageCarriesTheTimeItWasSent()
    {
        var kira = await _data.CreateCharacterAsync();
        factory.Clock.Advance(TimeSpan.FromDays(3));
        var before = factory.Clock.GetUtcNow();

        var sent = await _data.SendAsync(MessageTestData.Draft(kira, kira));

        Assert.InRange(sent.SentAtUtc, before, factory.Clock.GetUtcNow());
    }

    [Fact]
    public async Task RecipientListedTwiceIsKeptOnce()
    {
        var sender = await _data.CreateCharacterAsync("Kira");
        var recipient = await _data.CreateCharacterAsync("Jon");

        var sent = await _data.SendAsync(MessageTestData.Draft(sender, recipient, sender, recipient));

        Assert.Equal([recipient.CharacterId, sender.CharacterId], sent.Recipients.Select(character => character.CharacterId));
    }

    [Theory]
    [InlineData(CharacterStatus.Retired)]
    [InlineData(CharacterStatus.Dead)]
    public async Task CharactersOfAnyStatusCanSendAndReceive(CharacterStatus status)
    {
        var sender = await _data.CreateCharacterAsync("Kira", status);
        var recipient = await _data.CreateCharacterAsync("Jon", status);

        var result = await Messages.SendMessageAsync(MessageTestData.Draft(sender, recipient), Token);

        Assert.Equal(MessageOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task InvalidDraftIsRejectedAndNotStored()
    {
        var sender = await _data.CreateCharacterAsync();

        var result = await Messages.SendMessageAsync(MessageTestData.Draft(sender, sender) with { Body = " " }, Token);

        var stored = await Messages.GetMessagesAsync(new MessageQuery(null, 1), Token);
        Assert.Equal(new { Outcome = MessageOutcome.InvalidBody, Stored = 0L }, new { result.Outcome, Stored = stored.TotalCount });
    }

    [Fact]
    public async Task UnknownSenderAndRecipientsAreListed()
    {
        var recipient = await _data.CreateCharacterAsync();
        var unknownSender = Guid.NewGuid();
        var unknownRecipient = Guid.NewGuid();
        var draft = MessageTestData.Draft(recipient, recipient) with
        {
            SenderId = unknownSender,
            RecipientIds = [recipient.CharacterId, unknownRecipient]
        };

        var result = await Messages.SendMessageAsync(draft, Token);

        Assert.Equivalent(
            new { Outcome = MessageOutcome.UnknownCharacters, Ids = (Guid[])[unknownSender, unknownRecipient] },
            new { result.Outcome, Ids = result.CharacterIds.ToArray() },
            strict: true);
    }

    [Fact]
    public async Task ArchivedSenderIsRejected()
    {
        var sender = await _data.CreateArchivedCharacterAsync();
        var recipient = await _data.CreateCharacterAsync();

        var result = await Messages.SendMessageAsync(MessageTestData.Draft(sender, recipient), Token);

        Assert.Equivalent(
            new { Outcome = MessageOutcome.ArchivedCharacters, Ids = (Guid[])[sender.CharacterId] },
            new { result.Outcome, Ids = result.CharacterIds.ToArray() },
            strict: true);
    }

    [Fact]
    public async Task ArchivedRecipientIsRejected()
    {
        var sender = await _data.CreateCharacterAsync();
        var recipient = await _data.CreateArchivedCharacterAsync();

        var result = await Messages.SendMessageAsync(MessageTestData.Draft(sender, recipient), Token);

        Assert.Equivalent(
            new { Outcome = MessageOutcome.ArchivedCharacters, Ids = (Guid[])[recipient.CharacterId] },
            new { result.Outcome, Ids = result.CharacterIds.ToArray() },
            strict: true);
    }

    [Fact]
    public async Task ReplyRecordsItsSource()
    {
        var kira = await _data.CreateCharacterAsync("Kira");
        var jon = await _data.CreateCharacterAsync("Jon");
        var source = await _data.SendAsync(MessageTestData.Draft(kira, jon));

        var reply = await _data.SendAsync(MessageTestData.Draft(jon, kira) with
        {
            Title = "Re: Supply run",
            ReplyToMessageId = source.MessageId
        });

        Assert.Equal(new MessageReference(source.MessageId, "Supply run"), reply.ReplyTo);
    }

    [Fact]
    public async Task ReplyToAnUnknownMessageIsRejected()
    {
        var kira = await _data.CreateCharacterAsync();

        var result = await Messages.SendMessageAsync(MessageTestData.Draft(kira, kira) with { ReplyToMessageId = Guid.NewGuid() }, Token);

        Assert.Equal(MessageOutcome.UnknownReplySource, result.Outcome);
    }

    [Fact]
    public async Task RepeatedSubmissionReturnsTheMessageAlreadySent()
    {
        var kira = await _data.CreateCharacterAsync();
        var draft = MessageTestData.Draft(kira, kira);
        var first = await _data.SendAsync(draft);

        var repeated = await Messages.SendMessageAsync(draft, Token);

        Assert.Equivalent(first, repeated.Value, strict: true);
    }

    [Fact]
    public async Task RepeatedSubmissionSendsNothingNew()
    {
        var kira = await _data.CreateCharacterAsync();
        var draft = MessageTestData.Draft(kira, kira);
        await _data.SendAsync(draft);

        await Messages.SendMessageAsync(draft, Token);

        Assert.Single(await factory.LoadStreamEventsAsync(draft.MessageId));
    }

    [Fact]
    public async Task RepeatedSubmissionSucceedsAfterARecipientWasArchived()
    {
        var kira = await _data.CreateCharacterAsync("Kira");
        var jon = await _data.CreateCharacterAsync("Jon");
        var draft = MessageTestData.Draft(kira, jon);
        await _data.SendAsync(draft);
        var current = await _data.Characters.GetCharacterAsync(jon.CharacterId, Token);
        await _data.Characters.UpdateCharacterAsync(jon.CharacterId, current!.Version,
            Players.CharacterDefinitions.From(current) with { Status = CharacterStatus.Retired }, Token);
        await _data.Characters.ArchiveCharacterAsync(jon.CharacterId, Token);

        var repeated = await Messages.SendMessageAsync(draft, Token);

        Assert.Equal(MessageOutcome.Succeeded, repeated.Outcome);
    }

    [Fact]
    public async Task IdentifierOfAMessageWithDifferentContentIsInUse()
    {
        var kira = await _data.CreateCharacterAsync();
        var draft = MessageTestData.Draft(kira, kira);
        await _data.SendAsync(draft);

        var result = await Messages.SendMessageAsync(draft with { Body = "Changed my mind." }, Token);

        Assert.Equal(MessageOutcome.MessageIdInUse, result.Outcome);
    }

    [Fact]
    public async Task IdentifierOfAnotherStreamIsInUse()
    {
        var kira = await _data.CreateCharacterAsync();

        var result = await Messages.SendMessageAsync(MessageTestData.Draft(kira, kira) with { MessageId = kira.CharacterId }, Token);

        Assert.Equal(MessageOutcome.MessageIdInUse, result.Outcome);
    }

    [Fact]
    public async Task OverlappingRepeatedSubmissionsSendOnce()
    {
        var kira = await _data.CreateCharacterAsync();
        var draft = MessageTestData.Draft(kira, kira);

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Messages.SendMessageAsync(draft, Token)));

        Assert.Equivalent(
            new { Outcomes = Enumerable.Repeat(MessageOutcome.Succeeded, 4).ToArray(), Events = 1 },
            new { Outcomes = results.Select(result => result.Outcome).ToArray(), Events = (await factory.LoadStreamEventsAsync(draft.MessageId)).Count },
            strict: true);
    }

    [Fact]
    public async Task MessagesAreListedNewestFirst()
    {
        var kira = await _data.CreateCharacterAsync();
        foreach (var title in new[] { "One", "Two", "Three" })
        {
            await _data.SendAsync(MessageTestData.Draft(kira, kira) with { Title = title });
            factory.Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var page = await Messages.GetMessagesAsync(new MessageQuery(null, 1), Token);

        Assert.Equal(["Three", "Two", "One"], page.Items.Select(message => message.Title));
    }

    [Fact]
    public async Task ListShowsTheSenderAndRecipientCount()
    {
        var kira = await _data.CreateCharacterAsync("Kira");
        var jon = await _data.CreateCharacterAsync("Jon");
        var sent = await _data.SendAsync(MessageTestData.Draft(kira, kira, jon));

        var page = await Messages.GetMessagesAsync(new MessageQuery(null, 1), Token);

        Assert.Equivalent(
            new MessageSummary(sent.MessageId, sent.Sender, 2, "Supply run", sent.SentAtUtc, null),
            Assert.Single(page.Items),
            strict: true);
    }

    [Fact]
    public async Task MessagesArePagedByFifty()
    {
        var kira = await _data.CreateCharacterAsync();
        for (var index = 0; index < 51; index++)
        {
            await _data.SendAsync(MessageTestData.Draft(kira, kira));
        }

        var page = await Messages.GetMessagesAsync(new MessageQuery(null, 2), Token);

        Assert.Equal(
            new { Count = 1, PageNumber = 2, PageCount = 2, Total = 51L },
            new { page.Items.Count, page.PageNumber, page.PageCount, Total = page.TotalCount });
    }

    [Fact]
    public async Task EmptyListHasOneEmptyPage()
    {
        var page = await Messages.GetMessagesAsync(new MessageQuery(null, 1), Token);

        Assert.Equal(new { Count = 0, PageCount = 1 }, new { page.Items.Count, page.PageCount });
    }

    [Fact]
    public async Task CharacterFilterKeepsOnlyItsIncomingMessages()
    {
        var kira = await _data.CreateCharacterAsync("Kira");
        var jon = await _data.CreateCharacterAsync("Jon");
        await _data.SendAsync(MessageTestData.Draft(kira, jon) with { Title = "To Jon" });
        await _data.SendAsync(MessageTestData.Draft(jon, kira) with { Title = "To Kira" });
        await _data.SendAsync(MessageTestData.Draft(jon, jon, kira) with { Title = "To both" });

        var page = await Messages.GetMessagesAsync(new MessageQuery(kira.CharacterId, 1), Token);

        Assert.Equal(["To both", "To Kira"], page.Items.Select(message => message.Title).Order());
    }

    [Fact]
    public async Task ArchivedCharacterIsMarkedInTheDetails()
    {
        var kira = await _data.CreateCharacterAsync("Kira");
        var jon = await _data.CreateCharacterAsync("Jon");
        var sent = await _data.SendAsync(MessageTestData.Draft(kira, jon));
        var current = await _data.Characters.GetCharacterAsync(jon.CharacterId, Token);
        await _data.Characters.UpdateCharacterAsync(jon.CharacterId, current!.Version,
            Players.CharacterDefinitions.From(current) with { Status = CharacterStatus.Retired }, Token);
        await _data.Characters.ArchiveCharacterAsync(jon.CharacterId, Token);

        var opened = await Messages.OpenMessageAsync(sent.MessageId, Token);

        Assert.True(Assert.Single(opened.Value!.Recipients).IsArchived);
    }

    [Fact]
    public async Task CharacterThatCannotBeFoundIsShownAsUnknown()
    {
        var kira = await _data.CreateCharacterAsync();
        var missing = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        await using (var session = factory.Services.GetRequiredService<IDocumentStore>().LightweightSession())
        {
            // History written before a character could be found, for example by a later import.
            session.Events.StartStream(messageId,
                new MessageSent(messageId, kira.CharacterId, [missing], "Lost", "Body", null, factory.Clock.GetUtcNow()));
            await session.SaveChangesAsync(Token);
        }

        var opened = await Messages.OpenMessageAsync(messageId, Token);

        Assert.Equal(new MessageCharacter(missing, false, null, null, false), Assert.Single(opened.Value!.Recipients));
    }

    [Fact]
    public async Task OpeningAnUnknownMessageIsNotFound()
    {
        var result = await Messages.OpenMessageAsync(Guid.NewGuid(), Token);

        Assert.Equal(MessageOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task EveryOpeningIsRecorded()
    {
        var kira = await _data.CreateCharacterAsync();
        var sent = await _data.SendAsync(MessageTestData.Draft(kira, kira));

        await Messages.OpenMessageAsync(sent.MessageId, Token);
        await Messages.OpenMessageAsync(sent.MessageId, Token);

        Assert.Equal(
            [typeof(MessageSent), typeof(MessageOpened), typeof(MessageOpened)],
            (await factory.LoadStreamEventsAsync(sent.MessageId)).Select(stored => stored.EventType));
    }
}
