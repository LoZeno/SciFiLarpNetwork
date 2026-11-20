using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.CharacterCards;
using SciFiLarpNetwork.Backend.Infrastructure.CharacterCards;

namespace SciFiLarpNetwork.Backend.Tests.Infrastructure.CharacterCards;

public sealed class CardPrintingTests
{
    [Fact]
    public async Task MissingTypstExecutableIsARenderingFailure()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "typst");
        using var provider = CreateProvider(missing);
        var renderer = provider.GetRequiredService<ICharacterCardRenderer>();

        await Assert.ThrowsAsync<CardRenderingException>(() => renderer.RenderAsync(
            new CharacterCardDocument("en", new Dictionary<string, string>(), []), "x"u8.ToArray(),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TemplateThatRunsTooLongIsATemplateFailure()
    {
        var folder = Directory.CreateTempSubdirectory("scifilarp-slow-typst-");
        try
        {
            using var provider = CreateProvider(SlowTypst(folder.FullName), timeout: "00:00:01");
            var renderer = provider.GetRequiredService<ICharacterCardRenderer>();

            await Assert.ThrowsAsync<CardTemplateException>(() => renderer.RenderAsync(
                new CharacterCardDocument("en", new Dictionary<string, string>(), []), "x"u8.ToArray(),
                TestContext.Current.CancellationToken));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task TypstThatRunsTooLongLeavesNoWorkFolder()
    {
        var folder = Directory.CreateTempSubdirectory("scifilarp-slow-typst-");
        try
        {
            using var provider = CreateProvider(SlowTypst(folder.FullName), timeout: "00:00:01");
            var renderer = provider.GetRequiredService<ICharacterCardRenderer>();
            await Assert.ThrowsAsync<CardTemplateException>(() => renderer.RenderAsync(
                new CharacterCardDocument("en", new Dictionary<string, string>(), []), "x"u8.ToArray(),
                TestContext.Current.CancellationToken));

            var workFolder = (await File.ReadAllTextAsync(WorkFolderRecord(folder.FullName), TestContext.Current.CancellationToken)).Trim();

            Assert.False(Directory.Exists(workFolder));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static string WorkFolderRecord(string folder) => Path.Combine(folder, "work-folder.txt");

    // The fake Typst records the folder it runs in, which is the renderer's work folder, then hangs.
    private static string SlowTypst(string folder)
    {
        if (OperatingSystem.IsWindows())
        {
            var batch = Path.Combine(folder, "typst.cmd");
            File.WriteAllText(batch, $"@echo %CD%>\"{WorkFolderRecord(folder)}\"\r\n@ping -n 60 127.0.0.1 >nul\r\n");
            return batch;
        }

        var script = Path.Combine(folder, "typst");
        File.WriteAllText(script, $"#!/bin/sh\npwd > \"{WorkFolderRecord(folder)}\"\nsleep 60\n");
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return script;
    }

    private static ServiceProvider CreateProvider(string typstPath, string? timeout = null)
    {
        var configuration = new ConfigurationManager { ["CardPrinting:TypstPath"] = typstPath };
        if (timeout is not null)
        {
            configuration["CardPrinting:Timeout"] = timeout;
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCardPrinting(configuration);
        return services.BuildServiceProvider();
    }
}
