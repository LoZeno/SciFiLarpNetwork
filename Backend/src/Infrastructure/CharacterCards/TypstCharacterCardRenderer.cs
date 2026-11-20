using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QRCoder;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards.TemplateData;

namespace SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

/// <summary>
/// Renders cards with the Typst command-line compiler in a sandbox: the template is passed on standard input and
/// the Typst root is a fresh folder holding only <c>cards.json</c>, so a template can read nothing else. Packages
/// cannot be loaded and never trigger a download (see <see cref="CompileStartInfo"/>), and system fonts are ignored
/// so output does not depend on the host.
/// </summary>
internal sealed partial class TypstCharacterCardRenderer(
    IOptions<CardPrintingOptions> options,
    ILogger<TypstCharacterCardRenderer> logger) : ICharacterCardRenderer
{
    private const string DataFolderName = "data";
    private const string DataFileName = "cards.json";
    private const string OutputFileName = "cards.pdf";
    private const string MissingCertificateFileName = "no-certificate.pem";
    private const string UnreachableProxy = "http://127.0.0.1:9";
    private static readonly string[] ProxyVariables = ["HTTPS_PROXY", "https_proxy", "HTTP_PROXY", "http_proxy", "ALL_PROXY", "all_proxy"];
    private static readonly string[] NoProxyVariables = ["NO_PROXY", "no_proxy"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan StopWait = TimeSpan.FromSeconds(10);

    public async Task<byte[]> RenderAsync(CharacterCardDocument document, byte[] templateSource,
        CancellationToken cancellationToken = default)
    {
        var folder = Directory.CreateTempSubdirectory("scifilarp-cards-");
        try
        {
            var work = new TypstWorkFolder(folder.FullName);
            Directory.CreateDirectory(work.Data);
            Directory.CreateDirectory(work.Packages);
            Directory.CreateDirectory(work.PackageCache);
            await WriteDataAsync(work.Data, document, cancellationToken);
            await CompileAsync(work, templateSource, cancellationToken);
            return await File.ReadAllBytesAsync(work.Output, cancellationToken);
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

    private async Task CompileAsync(TypstWorkFolder work, byte[] templateSource, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = CompileStartInfo(work) };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new CardRenderingException($"Typst could not be started from '{process.StartInfo.FileName}'.", exception);
        }

        var input = WriteTemplateAsync(process, templateSource);
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
            await input;
            cancellationToken.ThrowIfCancellationRequested();
            throw new CardTemplateException($"Typst did not finish within {options.Value.Timeout}.");
        }

        await input;
        await output;
        if (process.ExitCode != 0)
        {
            var message = TypstWorkFolder.Hide(await errors);
            LogCompileFailed(process.ExitCode, message);
            throw new CardTemplateException(message.Trim());
        }
    }

    // A Typst that stops early (a failed start, a kill after the timeout) closes its input; that is not an error
    // of its own, the exit code and the timeout report it.
    private static async Task WriteTemplateAsync(Process process, byte[] template)
    {
        try
        {
            await process.StandardInput.BaseStream.WriteAsync(template);
            process.StandardInput.Close();
        }
        catch (IOException)
        {
        }
    }

    // Kill only requests termination; waiting lets the process release its files before the folder is deleted.
    // The wait is bounded so a kill that does not take effect can never hang the request.
    private async Task StopAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        using var wait = new CancellationTokenSource(StopWait);
        try
        {
            await process.WaitForExitAsync(wait.Token);
        }
        catch (OperationCanceledException)
        {
            LogStopTimedOut(StopWait);
        }
    }

    /// <remarks>
    /// Packages are blocked in three independent ways, so that no single one is relied on: the package folders
    /// are new and empty, so no package is found locally; <c>--cert</c> names a file that does not exist, so Typst
    /// fails before it starts a package download; and every proxy variable points at a closed local port, so a
    /// download that did start could not leave the computer.
    /// </remarks>
    private ProcessStartInfo CompileStartInfo(TypstWorkFolder work)
    {
        var startInfo = new ProcessStartInfo(TypstPath())
        {
            WorkingDirectory = work.Data,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        string[] arguments =
        [
            "--cert", work.MissingCertificate,
            "compile",
            "--root", work.Data,
            "--package-path", work.Packages,
            "--package-cache-path", work.PackageCache,
            "--ignore-system-fonts",
            "-", work.Output
        ];
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var name in startInfo.Environment.Keys
                     .Where(key => key.StartsWith("TYPST_", StringComparison.OrdinalIgnoreCase) || NoProxyVariables.Contains(key))
                     .ToArray())
        {
            startInfo.Environment.Remove(name);
        }

        foreach (var name in ProxyVariables)
        {
            startInfo.Environment[name] = UnreachableProxy;
        }

        return startInfo;
    }

    private string TypstPath() =>
        string.IsNullOrWhiteSpace(options.Value.TypstPath)
            ? Path.Combine(AppContext.BaseDirectory, "tools", "typst", OperatingSystem.IsWindows() ? "typst.exe" : "typst")
            : options.Value.TypstPath;

    private sealed record TypstWorkFolder(string Root)
    {
        public string Data => Path.Combine(Root, DataFolderName);

        public string Packages => Path.Combine(Root, "packages");

        public string PackageCache => Path.Combine(Root, "package-cache");

        public string MissingCertificate => Path.Combine(Root, MissingCertificateFileName);

        public string Output => Path.Combine(Root, OutputFileName);

        /// <summary>
        /// Removes the server's folder names from Typst's messages, which an administrator reads. Typst prints the
        /// folder in its canonical form (long names, symbolic links resolved), which can differ from
        /// <see cref="Root"/>, so the folder is found by its name prefix.
        /// </summary>
        public static string Hide(string message) => WorkFolderPath().Replace(message, string.Empty);
    }

    [GeneratedRegex(@"(?:\\\\\?\\)?(?:[A-Za-z]:[\\/]|/)[^\r\n<>""|?*]*?scifilarp-cards-[^\\/\r\n]+[\\/](?:data[\\/])?")]
    private static partial Regex WorkFolderPath();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Typst failed with exit code {ExitCode}: {Errors}")]
    private partial void LogCompileFailed(int exitCode, string errors);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Typst was killed but had not exited after {Wait}")]
    private partial void LogStopTimedOut(TimeSpan wait);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete the card rendering folder {Folder}")]
    private partial void LogCleanupFailed(string folder, Exception exception);
}
