using SciFiLarpNetwork.Backend.Application.Messages;

namespace SciFiLarpNetwork.Backend.Tests.Application.Messages;

public sealed class MessageDraftValidatorTests
{
    private static readonly Guid Sender = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Recipient = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static MessageDraft Valid => new(Guid.Parse("00000000-0000-0000-0000-0000000000aa"), Sender, [Recipient], "Supply run", "Meet at the dock.");

    public static TheoryData<MessageDraft, MessageOutcome> InvalidDrafts => new()
    {
        { Valid with { MessageId = Guid.Empty }, MessageOutcome.InvalidMessageId },
        { Valid with { Title = " " }, MessageOutcome.InvalidTitle },
        { Valid with { Title = new string('t', 129) }, MessageOutcome.InvalidTitle },
        { Valid with { Body = null }, MessageOutcome.InvalidBody },
        { Valid with { Body = new string('b', 10001) }, MessageOutcome.InvalidBody },
        { Valid with { RecipientIds = [] }, MessageOutcome.NoRecipients },
        { Valid with { RecipientIds = Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToArray() }, MessageOutcome.TooManyRecipients },
        { Valid with { Title = null, RecipientIds = [] }, MessageOutcome.InvalidTitle }
    };

    [Theory]
    [MemberData(nameof(InvalidDrafts))]
    public void InvalidDraftIsRejectedWithItsFirstFailedCheck(MessageDraft draft, MessageOutcome expected) =>
        Assert.Equal(expected, MessageDraftValidator.Validate(draft));

    [Fact]
    public void CompleteDraftIsValid() =>
        Assert.Null(MessageDraftValidator.Validate(Valid));

    [Fact]
    public void TwoHundredRecipientsAreValid() =>
        Assert.Null(MessageDraftValidator.Validate(Valid with { RecipientIds = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()).ToArray() }));

    [Fact]
    public void RecipientsListedTwiceCountOnce() =>
        Assert.Null(MessageDraftValidator.Validate(Valid with { RecipientIds = [.. Enumerable.Repeat(Recipient, 201)] }));

    [Fact]
    public void DistinctRecipientsKeepTheirFirstOrder() =>
        Assert.Equal([Recipient, Sender], (Valid with { RecipientIds = [Recipient, Sender, Recipient] }).DistinctRecipientIds);
}
