using System.Globalization;
using System.Net;
using System.Text.Json;
using SciFiLarpNetwork.Management.Api;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels.CardTemplates;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;

public sealed class CardTemplateScreenViewModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly byte[] PickedFile = "#let data = json(\"cards.json\")"u8.ToArray();

    [Fact]
    public async Task DefaultTemplateIsShownAsDefault()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs());

        Assert.Equal("Default", screen.CharacterTemplateState);
    }

    [Fact]
    public async Task UploadedTemplateShowsWhoAndWhen()
    {
        var uploadedAt = new DateTimeOffset(2026, 11, 20, 10, 30, 0, TimeSpan.Zero);
        var api = new FakeCardTemplateApi { IsDefault = false, UploadedBy = "local-admin", UploadedAtUtc = uploadedAt, Version = 1 };

        var screen = await OpenAsync(api, new FakeCardTemplateDialogs());

        Assert.Equal(
            $"Uploaded by local-admin, {uploadedAt.ToLocalTime().ToString("g", CultureInfo.GetCultureInfo("en"))}",
            screen.CharacterTemplateState);
    }

    [Fact]
    public async Task ItalianScreenShowsTheItalianState()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs(), "it");

        Assert.Equal("Predefinito", screen.CharacterTemplateState);
    }

    [Fact]
    public async Task ReaderCanDownload()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi { CanWrite = false }, new FakeCardTemplateDialogs());

        Assert.True(screen.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReaderCannotChangeThePreviewOrTheTemplate()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi { CanWrite = false }, new FakeCardTemplateDialogs());

        Assert.Equal(
            new { CanWrite = false, Preview = false, Upload = false, Reset = false },
            new
            {
                screen.CanWrite,
                Preview = screen.PreviewFileCommand.CanExecute(null),
                Upload = screen.UploadCommand.CanExecute(null),
                Reset = screen.ResetCommand.CanExecute(null)
            });
    }

    [Fact]
    public async Task ResetIsOffWhileTheDefaultTemplateIsUsed()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs());

        Assert.False(screen.ResetCommand.CanExecute(null));
    }

    [Fact]
    public async Task DownloadSavesTheCurrentTemplate()
    {
        var api = new FakeCardTemplateApi();
        var dialogs = new FakeCardTemplateDialogs();
        var screen = await OpenAsync(api, dialogs);

        await screen.DownloadCommand.ExecuteAsync();

        Assert.Equal(new { Content = (byte[]?)api.Source, Name = (string?)"character-cards.typ" },
            new { Content = dialogs.LastSavedTemplate, Name = dialogs.LastSavedFileName });
    }

    [Fact]
    public async Task CancelledDownloadSaysNothing()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs { SaveResult = false });

        await screen.DownloadCommand.ExecuteAsync();

        Assert.Null(screen.StatusMessage);
    }

    [Fact]
    public async Task PreviewSendsThePickedFile()
    {
        var api = new FakeCardTemplateApi();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.PreviewFileCommand.ExecuteAsync();

        Assert.Equal(new Submission("preview-card-template", Body(new { content = PickedFile })), api.Submissions[^1]);
    }

    [Fact]
    public async Task PreviewOpensThePdf()
    {
        var dialogs = new FakeCardTemplateDialogs { PickedTemplate = PickedFile };
        var screen = await OpenAsync(new FakeCardTemplateApi(), dialogs);

        await screen.PreviewFileCommand.ExecuteAsync();

        Assert.Same(FakeCardTemplateApi.PreviewPdf, dialogs.LastPreviewedPdf);
    }

    [Fact]
    public async Task PreviewWithoutAViewerSaysSo()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(),
            new FakeCardTemplateDialogs { PickedTemplate = PickedFile, PreviewResult = false });

        await screen.PreviewFileCommand.ExecuteAsync();

        Assert.Equal("No program could open the PDF.", screen.StatusMessage);
    }

    [Fact]
    public async Task CancelledPickSendsNothing()
    {
        var api = new FakeCardTemplateApi();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs());

        await screen.UploadCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task UploadSendsTheVersionAndTheFile()
    {
        var api = new FakeCardTemplateApi { Version = 3, IsDefault = false };
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal(new Submission("upload-card-template", Body(new { version = 3L, content = PickedFile })), api.Submissions[^1]);
    }

    [Fact]
    public async Task UploadShowsTheNewState()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.UploadCommand.ExecuteAsync();

        Assert.StartsWith("Uploaded by local-admin", screen.CharacterTemplateState, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadSaysItSaved()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal("Template saved.", screen.StatusMessage);
    }

    [Fact]
    public async Task FileOverTheLimitIsNotSent()
    {
        var api = new FakeCardTemplateApi();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs
        {
            PickedTemplate = new byte[CardTemplateScreenViewModel.MaximumTemplateByteCount + 1]
        });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task FileOverTheLimitSaysSo()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs
        {
            PickedTemplate = new byte[CardTemplateScreenViewModel.MaximumTemplateByteCount + 1]
        });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal("The template must be 256 KB or smaller.", screen.StatusMessage);
    }

    [Theory]
    [InlineData("upload")]
    [InlineData("preview")]
    public async Task EmptyFileIsNotSent(string command)
    {
        var api = new FakeCardTemplateApi();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = [] });

        await (command == "upload" ? screen.UploadCommand : screen.PreviewFileCommand).ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Theory]
    [InlineData("en", "The file is empty.")]
    [InlineData("it", "Il file è vuoto.")]
    public async Task EmptyFileShowsTheEmptyFileMessage(string locale, string message)
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs { PickedTemplate = [] }, locale);

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal(message, screen.TemplateError);
    }

    [Fact]
    public async Task EmptyFileShowsTheInvalidTemplateError()
    {
        var screen = await OpenAsync(new FakeCardTemplateApi(), new FakeCardTemplateDialogs { PickedTemplate = [] });

        await screen.UploadCommand.ExecuteAsync();

        Assert.StartsWith("Typst could not print the sample cards", screen.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileAtTheLimitIsSent()
    {
        var api = new FakeCardTemplateApi();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs
        {
            PickedTemplate = new byte[CardTemplateScreenViewModel.MaximumTemplateByteCount]
        });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Single(api.Submissions);
    }

    [Fact]
    public async Task RejectedUploadShowsTheTypstError()
    {
        var api = new FakeCardTemplateApi();
        api.Failures["upload-card-template"] = InvalidTemplate("error: package not found");
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal("error: package not found", screen.TemplateError);
    }

    [Fact]
    public async Task RejectedPreviewShowsTheTypstError()
    {
        var api = new FakeCardTemplateApi();
        api.Failures["preview-card-template"] = InvalidTemplate("error: file not found");
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.PreviewFileCommand.ExecuteAsync();

        Assert.Equal("error: file not found", screen.TemplateError);
    }

    [Fact]
    public async Task RejectedUploadShowsTheLocalizedMessage()
    {
        var api = new FakeCardTemplateApi();
        api.Failures["upload-card-template"] = InvalidTemplate("error: package not found");
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile }, "it");

        await screen.UploadCommand.ExecuteAsync();

        Assert.StartsWith("Typst non è riuscito", screen.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NextAttemptClearsTheTypstError()
    {
        var api = new FakeCardTemplateApi();
        api.Failures["upload-card-template"] = InvalidTemplate("error: package not found");
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });
        await screen.UploadCommand.ExecuteAsync();

        await screen.UploadCommand.ExecuteAsync();

        Assert.False(screen.HasTemplateError);
    }

    [Theory]
    [InlineData("errors.cardTemplateTooLarge", HttpStatusCode.BadRequest, "The template must be 256 KB or smaller.")]
    [InlineData("errors.cardTemplateNotSaved", HttpStatusCode.ServiceUnavailable, "The template change could not be saved. Try again later.")]
    [InlineData("errors.staleVersion", HttpStatusCode.Conflict, "Someone else changed this item. Reload it and try again.")]
    public async Task UploadErrorIsShown(string code, HttpStatusCode status, string message)
    {
        var api = new FakeCardTemplateApi();
        api.Failures["upload-card-template"] = new ManagementApiException(code, status);
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal(message, screen.StatusMessage);
    }

    [Fact]
    public async Task StaleUploadLoadsTheLatestTemplate()
    {
        var api = new FakeCardTemplateApi();
        api.Failures["upload-card-template"] = new ManagementApiException("errors.staleVersion", HttpStatusCode.Conflict);
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { PickedTemplate = PickedFile });

        await screen.UploadCommand.ExecuteAsync();

        Assert.Equal(2, api.TemplateLoads);
    }

    [Fact]
    public async Task ResetAsksWithTheAgreedWording()
    {
        var dialogs = new FakeCardTemplateDialogs();
        var screen = await OpenAsync(Uploaded(), dialogs);

        await screen.ResetCommand.ExecuteAsync();

        Assert.Equal(
            "Use the default character-card template again? The uploaded template is removed.",
            dialogs.LastConfirmation?.Message);
    }

    [Fact]
    public async Task ResetAsksInItalian()
    {
        var dialogs = new FakeCardTemplateDialogs();
        var screen = await OpenAsync(Uploaded(), dialogs, "it");

        await screen.ResetCommand.ExecuteAsync();

        Assert.Equal(
            "Usare di nuovo il modello predefinito dei cartellini dei personaggi? Il modello caricato viene rimosso.",
            dialogs.LastConfirmation?.Message);
    }

    [Fact]
    public async Task CancelledResetSendsNothing()
    {
        var api = Uploaded();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs { ConfirmResult = false });

        await screen.ResetCommand.ExecuteAsync();

        Assert.Empty(api.Submissions);
    }

    [Fact]
    public async Task ConfirmedResetSendsTheVersion()
    {
        var api = Uploaded();
        var screen = await OpenAsync(api, new FakeCardTemplateDialogs());

        await screen.ResetCommand.ExecuteAsync();

        Assert.Equal(new Submission("reset-card-template", Body(new { version = 1L })), api.Submissions[^1]);
    }

    [Fact]
    public async Task ResetShowsTheDefaultAgain()
    {
        var screen = await OpenAsync(Uploaded(), new FakeCardTemplateDialogs());

        await screen.ResetCommand.ExecuteAsync();

        Assert.Equal(
            new { State = "Default", Status = (string?)"The default template is used again." },
            new { State = screen.CharacterTemplateState, Status = screen.StatusMessage });
    }

    private static FakeCardTemplateApi Uploaded() =>
        new() { IsDefault = false, UploadedBy = "local-admin", UploadedAtUtc = DateTimeOffset.UnixEpoch, Version = 1 };

    private static ManagementApiException InvalidTemplate(string typstError) =>
        new("errors.invalidCardTemplate", HttpStatusCode.BadRequest,
            new Dictionary<string, IReadOnlyList<string>> { ["template"] = [typstError] });

    private static string Body(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static async Task<CardTemplateScreenViewModel> OpenAsync(FakeCardTemplateApi api, FakeCardTemplateDialogs dialogs,
        string locale = "en")
    {
        var screen = new CardTemplateScreenViewModel(api, new JsonLocalizer(), dialogs, "http://localhost/api", locale,
            FakeCardTemplateApi.TemplatesHref);
        await screen.LoadAsync();
        return screen;
    }
}
