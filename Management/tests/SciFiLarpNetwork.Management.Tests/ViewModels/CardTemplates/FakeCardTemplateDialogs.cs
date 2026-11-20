using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.CardTemplates;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;

/// <summary>Plays the user's part in each card template dialog.</summary>
internal sealed class FakeCardTemplateDialogs : ICardTemplateDialogService
{
    public bool ConfirmResult { get; init; } = true;

    /// <summary>The file the user picks, or <see langword="null"/> to cancel.</summary>
    public byte[]? PickedTemplate { get; init; }

    public bool SaveResult { get; init; } = true;

    public bool PreviewResult { get; init; } = true;

    public ConfirmationRequest? LastConfirmation { get; private set; }

    public byte[]? LastSavedTemplate { get; private set; }

    public string? LastSavedFileName { get; private set; }

    public byte[]? LastPreviewedPdf { get; private set; }

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        LastConfirmation = request;
        return Task.FromResult(ConfirmResult);
    }

    public Task<byte[]?> PickTemplateAsync() => Task.FromResult(PickedTemplate);

    public Task<bool> SaveTemplateAsync(byte[] content, string suggestedFileName, string fileTypeName)
    {
        LastSavedTemplate = content;
        LastSavedFileName = suggestedFileName;
        return Task.FromResult(SaveResult);
    }

    public Task<bool> PreviewPdfAsync(byte[] pdf, string fileName)
    {
        LastPreviewedPdf = pdf;
        return Task.FromResult(PreviewResult);
    }
}
