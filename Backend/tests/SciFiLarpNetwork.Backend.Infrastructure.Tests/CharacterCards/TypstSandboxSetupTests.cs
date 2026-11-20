using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure.CharacterCards;

/// <summary>
/// Checks each independent package and file block by what Typst is given: a fake Typst records its arguments, its
/// environment, and the files of its work folder, then fails. The real Typst's behavior is covered by the
/// integration tests; these tests show that no single block can be removed unnoticed.
/// </summary>
public sealed class TypstSandboxSetupTests : IDisposable
{
    private readonly DirectoryInfo _records = Directory.CreateTempSubdirectory("scifilarp-typst-record-");

    public void Dispose() => _records.Delete(recursive: true);

    [Fact]
    public async Task TypstRootHoldsOnlyTheCardData()
    {
        var run = await RunFakeTypstAsync();

        Assert.Equal(["cards.json"], run.FilesUnder(run.Argument("--root")));
    }

    [Fact]
    public async Task TemplateIsReadFromStandardInput()
    {
        var run = await RunFakeTypstAsync();

        Assert.Equal("-", run.Arguments[^2]);
    }

    [Fact]
    public async Task OutputIsWrittenOutsideTheRoot()
    {
        var run = await RunFakeTypstAsync();

        Assert.False(run.IsInside(run.Arguments[^1], run.Argument("--root")));
    }

    [Fact]
    public async Task LocalPackageFolderIsEmpty()
    {
        var run = await RunFakeTypstAsync();

        Assert.Equal(["(empty)"], run.FilesUnderOrEmpty(run.Argument("--package-path")));
    }

    [Fact]
    public async Task PackageCacheFolderIsEmpty()
    {
        var run = await RunFakeTypstAsync();

        Assert.Equal(["(empty)"], run.FilesUnderOrEmpty(run.Argument("--package-cache-path")));
    }

    [Fact]
    public async Task CertificateFileDoesNotExist()
    {
        var run = await RunFakeTypstAsync();

        Assert.False(run.Exists(run.Argument("--cert")));
    }

    [Theory]
    [InlineData("HTTPS_PROXY")]
    [InlineData("HTTP_PROXY")]
    [InlineData("ALL_PROXY")]
    public async Task ProxyPointsAtAClosedLocalPort(string name)
    {
        var run = await RunFakeTypstAsync();

        Assert.Equal("http://127.0.0.1:9", run.Environment(name));
    }

    [Fact]
    public async Task NoProxyExceptionsArePassedOn()
    {
        var run = await RunFakeTypstAsync(("NO_PROXY", "*"));

        Assert.Null(run.Environment("NO_PROXY"));
    }

    [Fact]
    public async Task TypstSettingsFromTheEnvironmentAreNotPassedOn()
    {
        var run = await RunFakeTypstAsync(("TYPST_PACKAGE_PATH", "/somewhere"));

        Assert.Null(run.Environment("TYPST_PACKAGE_PATH"));
    }

    private async Task<FakeTypstRun> RunFakeTypstAsync(params (string Name, string Value)[] environment)
    {
        foreach (var (name, value) in environment)
        {
            System.Environment.SetEnvironmentVariable(name, value);
        }

        try
        {
            using var provider = CreateProvider(RecordingTypst());
            await Assert.ThrowsAsync<CardTemplateException>(() => provider.GetRequiredService<ICharacterCardRenderer>()
                .RenderAsync(new CharacterCardDocument("en", new Dictionary<string, string>(), []), "x"u8.ToArray(),
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            foreach (var (name, _) in environment)
            {
                System.Environment.SetEnvironmentVariable(name, null);
            }
        }

        return await FakeTypstRun.ReadAsync(_records.FullName);
    }

    // Records each argument on its own line, the environment, the work folder (the parent of the folder Typst
    // runs in), and every file and folder under it, then fails like a Typst compile error.
    private string RecordingTypst()
    {
        var folder = _records.FullName;
        if (OperatingSystem.IsWindows())
        {
            var batch = Path.Combine(folder, "typst.cmd");
            File.WriteAllText(batch, $"""
                @echo off
                set > "{folder}\env.txt"
                :arguments
                if "%~1"=="" goto files
                >>"{folder}\args.txt" echo(%~1
                shift
                goto arguments
                :files
                cd ..
                >"{folder}\work.txt" echo(%CD%
                dir /s /b /a > "{folder}\files.txt"
                exit /b 1
                """.ReplaceLineEndings("\r\n"));
            return batch;
        }

        var script = Path.Combine(folder, "typst");
        File.WriteAllText(script, $"""
            #!/bin/sh
            env > "{folder}/env.txt"
            for argument in "$@"; do printf '%s\n' "$argument" >> "{folder}/args.txt"; done
            cd ..
            pwd -P > "{folder}/work.txt"
            find "$(pwd -P)" -mindepth 1 > "{folder}/files.txt"
            exit 1
            """.ReplaceLineEndings("\n"));
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return script;
    }

    private static ServiceProvider CreateProvider(string typstPath)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCardPrinting(new ConfigurationManager { ["CardPrinting:TypstPath"] = typstPath });
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// What the fake Typst saw. Paths in the arguments are compared by their place in the work folder, because the
    /// script may print the folder in another form (for example with symbolic links resolved).
    /// </summary>
    private sealed record FakeTypstRun(string[] Arguments, string[] EnvironmentLines, string WorkFolder, string[] Files)
    {
        public static async Task<FakeTypstRun> ReadAsync(string folder)
        {
            var arguments = await File.ReadAllLinesAsync(Path.Combine(folder, "args.txt"), TestContext.Current.CancellationToken);
            var environment = await File.ReadAllLinesAsync(Path.Combine(folder, "env.txt"), TestContext.Current.CancellationToken);
            var work = (await File.ReadAllTextAsync(Path.Combine(folder, "work.txt"), TestContext.Current.CancellationToken)).Trim();
            var files = await File.ReadAllLinesAsync(Path.Combine(folder, "files.txt"), TestContext.Current.CancellationToken);
            return new FakeTypstRun(arguments, environment, work,
                files.Where(line => line.Length > 0).Select(file => Relative(work, file)).ToArray());
        }

        public string Argument(string name) =>
            Array.IndexOf(Arguments, name) is var index and >= 0
                ? Arguments[index + 1]
                : throw new InvalidOperationException($"Typst was not given {name}.");

        public string? Environment(string name) =>
            EnvironmentLines
                .Where(line => line.StartsWith(name + "=", OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
                .Select(line => line[(name.Length + 1)..])
                .FirstOrDefault();

        public string[] FilesUnder(string argumentPath)
        {
            var folder = InWorkFolder(argumentPath) + Path.DirectorySeparatorChar;
            return Files.Where(file => file.StartsWith(folder, StringComparison.Ordinal))
                .Select(file => file[folder.Length..])
                .ToArray();
        }

        public string[] FilesUnderOrEmpty(string argumentPath) => FilesUnder(argumentPath) is { Length: > 0 } files ? files : ["(empty)"];

        public bool Exists(string argumentPath) => Files.Contains(InWorkFolder(argumentPath));

        public bool IsInside(string argumentPath, string folderArgumentPath) =>
            InWorkFolder(argumentPath).StartsWith(InWorkFolder(folderArgumentPath) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

        // The work folder is the parent of the Typst root, in the form the renderer passed it.
        private string InWorkFolder(string argumentPath) =>
            Path.GetRelativePath(Path.GetDirectoryName(Argument("--root"))!, argumentPath);

        private static string Relative(string work, string file) => Path.GetRelativePath(work, file);
    }
}
