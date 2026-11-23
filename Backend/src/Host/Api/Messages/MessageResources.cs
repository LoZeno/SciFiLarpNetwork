using SciFiLarpNetwork.Backend.Application.Messages;
using SciFiLarpNetwork.Backend.Domain.Messages;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

namespace SciFiLarpNetwork.Backend.Api.Messages;

/// <summary>
/// Builds the Siren representations of messages. The send and reply actions are advertised only when
/// <paramref name="canWrite"/> is set; the server still authorizes and validates every request.
/// </summary>
internal sealed class MessageResources(IApplicationText text, string pathBase, bool canWrite)
{
    private const string JsonType = "application/json";

    public string MessagesPath => $"{pathBase}/api/v1/messages";

    public string MessagePath(Guid messageId) => $"{MessagesPath}/{messageId:D}";

    private string CharactersPath => $"{pathBase}/api/v1/characters";

    public SirenEntity<MessageCollectionProperties> MessageCollection(MessagePage page, MessageQuery query) =>
        new()
        {
            Class = ["message-collection"],
            Properties = new MessageCollectionProperties(
                text.GetLocalizedText("api.messages"),
                text.GetLocalizedText("api.messagesDescription"),
                query.CharacterId,
                page.PageNumber,
                page.PageCount,
                page.TotalCount,
                MessageTexts.MaximumRecipients,
                MessageTexts.MaximumTitleLength,
                MessageTexts.MaximumBodyLength),
            Entities = page.Items.Select(message => new SirenSubEntity
            {
                Rel = ["item"],
                Class = ["message"],
                Href = MessagePath(message.MessageId),
                Properties = SirenJson.ToElement(message)
            }).ToArray(),
            Links = PageLinks(page, query),
            Actions =
            [
                new SirenAction
                {
                    Name = "filter-messages",
                    Href = MessagesPath,
                    Method = "GET",
                    Title = text.GetLocalizedText("api.filterMessages"),
                    Fields = [Field("characterId", "hidden", "messages.character", query.CharacterId)]
                },
                .. canWrite ? [SendAction("send-message", "api.sendMessage", null)] : Array.Empty<SirenAction>()
            ]
        };

    public SirenEntity<MessageDetails> Message(MessageDetails message)
    {
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = MessagePath(message.MessageId) },
            new() { Rel = ["collection"], Href = MessagesPath },
            new() { Rel = [SirenRelations.Characters], Href = CharactersPath }
        };
        if (message.ReplyTo is { } source)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.ReplySource], Href = MessagePath(source.MessageId) });
        }

        return new SirenEntity<MessageDetails>
        {
            Class = ["message"],
            Properties = message,
            Links = links,
            Actions = canWrite ? [SendAction("reply-to-message", "api.replyToMessage", message.MessageId)] : null
        };
    }

    /// <summary>A reply is a message sent with the source filled in; the client fills the other fields.</summary>
    private SirenAction SendAction(string name, string titleKey, Guid? replyToMessageId) =>
        new()
        {
            Name = name,
            Href = MessagesPath,
            Method = "POST",
            Type = JsonType,
            Title = text.GetLocalizedText(titleKey),
            Fields =
            [
                Field("messageId", "hidden", "messages.messageId", (Guid?)null),
                Field("senderId", "hidden", "messages.sender", (Guid?)null),
                Field("recipientIds", "hidden", "messages.recipients", Array.Empty<Guid>()),
                Field("title", "text", "messages.title", (string?)null),
                Field("body", "textarea", "messages.body", (string?)null),
                Field("replyToMessageId", "hidden", "messages.replyTo", replyToMessageId)
            ]
        };

    private SirenLink[] PageLinks(MessagePage page, MessageQuery query)
    {
        var links = new List<SirenLink>
        {
            new() { Rel = ["self"], Href = PageHref(query) },
            new() { Rel = [SirenRelations.First], Href = PageHref(query with { PageNumber = 1 }) },
            new() { Rel = [SirenRelations.Last], Href = PageHref(query with { PageNumber = page.PageCount }) },
            new() { Rel = [SirenRelations.Characters], Href = CharactersPath }
        };
        if (page.PageNumber > 1)
        {
            links.Add(new SirenLink
            {
                Rel = [SirenRelations.Previous],
                Href = PageHref(query with { PageNumber = Math.Min(page.PageNumber - 1, page.PageCount) })
            });
        }

        if (page.PageNumber < page.PageCount)
        {
            links.Add(new SirenLink { Rel = [SirenRelations.Next], Href = PageHref(query with { PageNumber = page.PageNumber + 1 }) });
        }

        return [.. links];
    }

    private string PageHref(MessageQuery query)
    {
        var parameters = new List<string>();
        if (query.CharacterId is { } characterId)
        {
            parameters.Add($"characterId={characterId:D}");
        }

        if (query.PageNumber > 1)
        {
            parameters.Add($"page={query.PageNumber}");
        }

        return parameters.Count == 0 ? MessagesPath : $"{MessagesPath}?{string.Join('&', parameters)}";
    }

    private SirenField Field<T>(string name, string type, string titleKey, T? value) =>
        new()
        {
            Name = name,
            Type = type,
            Title = text.GetLocalizedText(titleKey),
            Value = value is null ? null : SirenJson.ToElement(value)
        };
}
