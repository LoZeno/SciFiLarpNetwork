using SciFiLarpNetwork.Management.Tests.ViewModels.CardTemplates;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SciFiLarpNetwork.Management.Tests.ViewModels.Players;
using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.Settings;
using SciFiLarpNetwork.Management.Tests.ViewModels.Networks;
using SciFiLarpNetwork.Management.Tests.ViewModels.Skills;
using SciFiLarpNetwork.Management.ViewModels;
using SciFiLarpNetwork.Management.ViewModels.Networks;
using SciFiLarpNetwork.Management.Views;
using SciFiLarpNetwork.Management.Views.Converters;
using SciFiLarpNetwork.Management.Views.Networks;

namespace SciFiLarpNetwork.Management.Tests.Views;

public sealed class NetworkCatalogWindowTests
{
    [AvaloniaFact]
    public async Task NewNetworkOpensTheNetworkDialogOverTheOwner()
    {
        var owner = ShowOwner();
        var catalog = CreateCatalog(new WindowNetworkDialogService(() => owner));
        await catalog.LoadAsync();

        var running = catalog.NewNetworkCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var dialog = owner.OwnedWindows.Single();
        var title = dialog.Title;
        dialog.Close();
        await running;

        Assert.Equal(new { Type = typeof(NetworkEditorWindow), Title = (string?)"New network" }, new { Type = dialog.GetType(), Title = title });
    }

    [AvaloniaFact]
    public async Task SavingTheNetworkDialogClosesIt()
    {
        var owner = ShowOwner();
        var catalog = CreateCatalog(new WindowNetworkDialogService(() => owner));
        await catalog.LoadAsync();
        var running = catalog.NewNetworkCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        var editor = (NetworkEditorViewModel)owner.OwnedWindows.Single().DataContext!;
        editor.Code = "rim";
        editor.Name = "Outer Rim";

        await editor.SaveCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        await running;

        Assert.Empty(owner.OwnedWindows);
    }

    [AvaloniaFact]
    public async Task NetworkConfirmationUsesTheConfirmationWindow()
    {
        var owner = ShowOwner();
        var asking = new WindowNetworkDialogService(() => owner)
            .ConfirmAsync(new ConfirmationRequest("Please confirm", "Disable?", "Disable", "Cancel"));
        Dispatcher.UIThread.RunJobs();
        var dialog = owner.OwnedWindows.Single();

        dialog.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new { Type = typeof(ConfirmationWindow), Confirmed = false }, new { Type = dialog.GetType(), Confirmed = await asking });
    }

    [AvaloniaFact]
    public void ImageConverterDecodesAPng()
    {
        var bitmap = ImageBytesConverter.Instance.Convert(TinyPng(), typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Equal(new PixelSize(1, 1), ((Bitmap)bitmap!).PixelSize);
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void ImageConverterShowsNothingForMissingOrBrokenImages(byte[]? content)
    {
        Assert.Null(ImageBytesConverter.Instance.Convert(content, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [AvaloniaFact]
    public void ImageConverterShowsNothingForAPngThatDeclaresAHugeSize()
    {
        byte[] hostile =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0xEA, 0x60, 0x00, 0x00, 0xEA, 0x60, 0x08, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];

        Assert.Null(ImageBytesConverter.Instance.Convert(hostile, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [AvaloniaFact]
    public void ImageConverterCannotConvertBack()
    {
        Assert.Throws<NotSupportedException>(() =>
            ImageBytesConverter.Instance.ConvertBack(null, typeof(byte[]), null, CultureInfo.InvariantCulture));
    }

    [AvaloniaFact]
    public async Task MainWindowShowsTheNetworksScreenWhenItsEntryIsSelected()
    {
        var api = new FakeNetworkApi();
        api.AddNetwork("rim", "Outer Rim");
        var viewModel = new MainWindowViewModel(
            api,
            new NullSettingsStore(),
            new JsonLocalizer(),
            new FakeCatalogDialogs(),
            new FakeNetworkDialogs(),
            new FakePlayerDialogs(),
            new FakeCardTemplateDialogs(),
            new ManagementSettings());
        var window = new MainWindow(viewModel);
        window.Show();
        await viewModel.ConnectCommand.ExecuteAsync();

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[0];
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.GetVisualDescendants().OfType<NetworkCatalogView>().Single().IsEffectivelyVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void NetworkEditorWindowShowsThePreviewImage()
    {
        var window = OpenEditorWindowWithImage(TinyPng());

        var image = window.GetVisualDescendants().OfType<Image>().Single();

        Assert.Equal(new PixelSize(1, 1), ((Bitmap)image.Source!).PixelSize);
        window.Close();
    }

    private static NetworkEditorWindow OpenEditorWindowWithImage(byte[] content)
    {
        var api = new FakeNetworkApi();
        api.AddImage(api.AddNetwork("rim", "Outer Rim"), content);
        NetworkEditorWindow? shown = null;
        var dialogs = new FakeNetworkDialogs
        {
            OnNetworkEditor = editor =>
            {
                shown = new NetworkEditorWindow(editor);
                shown.Show();
                Dispatcher.UIThread.RunJobs();
                return Task.CompletedTask;
            }
        };
        var catalog = new NetworkCatalogViewModel(api, new JsonLocalizer(), dialogs, "http://localhost:5000/api", "en", FakeNetworkApi.NetworksHref);
        catalog.LoadAsync().GetAwaiter().GetResult();
        catalog.SelectedNetwork = catalog.Networks[0];
        catalog.EditNetworkCommand.ExecuteAsync().GetAwaiter().GetResult();
        return shown!;
    }

    /// <summary>A valid 1 × 1 pixel PNG file.</summary>
    private static byte[] TinyPng() =>
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private static Window ShowOwner()
    {
        var owner = new Window();
        owner.Show();
        return owner;
    }

    private static NetworkCatalogViewModel CreateCatalog(INetworkDialogService dialogs) =>
        new(new FakeNetworkApi(), new JsonLocalizer(), dialogs, "http://localhost:5000/api", "en", FakeNetworkApi.NetworksHref);

    private sealed class NullSettingsStore : IManagementSettingsStore
    {
        public ManagementSettings Load() => new();

        public void Save(ManagementSettings settings)
        {
        }
    }
}
