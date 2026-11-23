using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.LarpEvents;
using SciFiLarpNetwork.Management.ViewModels.Messages;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.Messages;

public sealed class MessageEditorViewModelTests
{
    private static readonly CharacterChoice Kira = new(FakeMessageApi.KiraId, "#1 Kira");
    private static readonly CharacterChoice Odo = new(FakeMessageApi.OdoId, "#2 Odo");

    [Fact]
    public void RecipientAddedTwiceIsListedOnce()
    {
        var editor = CreateEditor();

        editor.AddRecipient(Kira);
        editor.AddRecipient(Kira);

        Assert.Equal([Kira], editor.Recipients);
    }

    [Fact]
    public async Task FoundCharacterAlreadyListedCannotBeAddedAgain()
    {
        var editor = CreateEditor();
        editor.SearchText = "Kira";
        await editor.SearchCommand.ExecuteAsync();

        editor.AddRecipientCommand.Execute(null);

        Assert.False(editor.AddRecipientCommand.CanExecute(null));
    }

    [Fact]
    public void FullRecipientListTakesNoMore()
    {
        var editor = CreateEditor(maximumRecipients: 1);
        editor.AddRecipient(Kira);

        editor.AddRecipient(Odo);

        Assert.Equal(new { Recipients = 1, Count = "1 of 1" }, new { Recipients = editor.Recipients.Count, Count = editor.RecipientCountText });
    }

    [Fact]
    public void RemovingTheSelectedRecipientTakesItOffTheList()
    {
        var editor = CreateEditor();
        editor.AddRecipient(Kira);
        editor.AddRecipient(Odo);
        editor.SelectedRecipient = Kira;

        editor.RemoveRecipientCommand.Execute(null);

        Assert.Equal([Odo], editor.Recipients);
    }

    [Fact]
    public async Task SenderIsRequired()
    {
        var editor = CreateEditor();
        editor.AddRecipient(Odo);

        await editor.SaveCommand.ExecuteAsync();

        Assert.Equal("Choose a sender.", editor.ErrorMessage);
    }

    [Fact]
    public async Task AtLeastOneRecipientIsRequired()
    {
        var editor = CreateEditor();
        editor.Sender = Kira;

        await editor.SaveCommand.ExecuteAsync();

        Assert.Equal("Choose at least one recipient.", editor.ErrorMessage);
    }

    [Fact]
    public void NoSenderChosenIsShown() =>
        Assert.Equal("No sender chosen yet.", CreateEditor().SenderText);

    [Fact]
    public async Task SearchWithoutResultsSaysSo()
    {
        var editor = CreateEditor();
        editor.SearchText = "Nobody";

        await editor.SearchCommand.ExecuteAsync();

        Assert.Equal("No results found.", editor.SearchMessage);
    }

    [Fact]
    public void FailedSearchSaysSomethingWentWrong()
    {
        var editor = new MessageEditorViewModel(new JsonLocalizer(), "en", "messages.newMessage", new MessageLimits(200, 128, 10000), null,
            _ => Task.FromException<IReadOnlyList<CharacterChoice>>(new HttpRequestException("Offline.")),
            _ => Task.FromResult(true));
        editor.SearchText = "Kira";

        editor.SearchCommand.Execute(null);

        Assert.Equal("Something went wrong.", editor.SearchMessage);
    }

    private static MessageEditorViewModel CreateEditor(int maximumRecipients = 200) =>
        new(new JsonLocalizer(), "en", "messages.newMessage", new MessageLimits(maximumRecipients, 128, 10000), null,
            text => Task.FromResult<IReadOnlyList<CharacterChoice>>(
                [.. new[] { Kira, Odo }.Where(character => character.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))]),
            _ => Task.FromResult(true));
}
