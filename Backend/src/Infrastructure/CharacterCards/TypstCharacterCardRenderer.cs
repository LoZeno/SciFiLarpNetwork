using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QRCoder;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.TemplateData;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

/// <summary>
/// Renders cards with the Typst command-line compiler: the template and <c>cards.json</c> are written to a fresh
/// folder that is also the Typst root, so a template can read nothing else, and system fonts are ignored so output
/// does not depend on the host.
/// </summary>
internal sealed partial class TypstCharacterCardRenderer(
    IOptions<CardPrintingOptions> options,
    ILogger<TypstCharacterCardRenderer> logger) : ICharacterCardRenderer
{
    private const string TemplateFileName = "character-cards.typ";
    private const string DataFileName = "cards.json";
    private const string OutputFileName = "cards.pdf";
    private const string DefaultTemplateResource = "SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.character-cards.typ";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<byte[]> RenderAsync(CharacterCardDocument document, CancellationToken cancellationToken = default)
    {
        var folder = Directory.CreateTempSubdirectory("scifilarp-cards-");
        try
        {
            await WriteTemplateAsync(folder.FullName, cancellationToken);
            await WriteDataAsync(folder.FullName, document, cancellationToken);
            await CompileAsync(folder.FullName, cancellationToken);
            return await File.ReadAllBytesAsync(Path.Combine(folder.FullName, OutputFileName), cancellationToken);
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    // Cleanup never changes the outcome: a file still held (for example by a virus scanner) must not turn a
    // finished PDF or a rendering error into an unrelated failure.
    private void DeleteFolder(DirectoryInfo folder)
    {
        try
        {
            folder.Delete(recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogCleanupFailed(folder.FullName, exception);
        }
    }

    private static async Task WriteTemplateAsync(string folder, CancellationToken cancellationToken)
    {
        await using var template = typeof(TypstCharacterCardRenderer).Assembly.GetManifestResourceStream(DefaultTemplateResource)
            ?? throw new InvalidOperationException($"The embedded resource '{DefaultTemplateResource}' is missing.");
        await using var file = File.Create(Path.Combine(folder, TemplateFileName));
        await template.CopyToAsync(file, cancellationToken);
    }

    private static async Task WriteDataAsync(string folder, CharacterCardDocument document, CancellationToken cancellationToken)
    {
        var data = new TemplateDocument(
            document.Language,
            document.Labels,
            document.Cards.Select(card => TemplateCard.From(card, QrSvg(card.QrIdentifier))).ToArray());
        await using var file = File.Create(Path.Combine(folder, DataFileName));
        await JsonSerializer.SerializeAsync(file, data, JsonOptions, cancellationToken);
    }

    private static string QrSvg(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(data);
        return svg.GetGraphic(4);
    }

    private async Task CompileAsync(string folder, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = CompileStartInfo(folder) };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new CardRenderingException($"Typst could not be started from '{process.StartInfo.FileName}'.", exception);
        }

        var errors = process.StandardError.ReadToEndAsync(cancellationToken);
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            await StopAsync(process);
            cancellationToken.ThrowIfCancellationRequested();
            throw new CardRenderingException($"Typst did not finish within {options.Value.Timeout}.");
        }

        await output;
        if (process.ExitCode != 0)
        {
            var message = await errors;
            LogCompileFailed(process.ExitCode, message);
            throw new CardRenderingException($"Typst failed with exit code {process.ExitCode}: {message}");
        }
    }

    // Kill only requests termination; waiting lets the process release its files before the folder is deleted.
    private static async Task StopAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        await process.WaitForExitAsync(CancellationToken.None);
    }

    private ProcessStartInfo CompileStartInfo(string folder)
    {
        var startInfo = new ProcessStartInfo(TypstPath())
        {
            WorkingDirectory = folder,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in (string[])["compile", "--root", folder, "--ignore-system-fonts", TemplateFileName, OutputFileName])
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var name in startInfo.Environment.Keys.Where(key => key.StartsWith("TYPST_", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            startInfo.Environment.Remove(name);
        }

        return startInfo;
    }

    private string TypstPath() =>
        string.IsNullOrWhiteSpace(options.Value.TypstPath)
            ? Path.Combine(AppContext.BaseDirectory, "tools", "typst", OperatingSystem.IsWindows() ? "typst.exe" : "typst")
            : options.Value.TypstPath;

    [LoggerMessage(Level = LogLevel.Error, Message = "Typst failed with exit code {ExitCode}: {Errors}")]
    private partial void LogCompileFailed(int exitCode, string errors);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete the card rendering folder {Folder}")]
    private partial void LogCleanupFailed(string folder, Exception exception);
}
