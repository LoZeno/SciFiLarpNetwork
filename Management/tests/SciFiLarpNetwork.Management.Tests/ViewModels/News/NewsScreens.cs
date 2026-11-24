using SciFiLarpNetwork.Management.Localization;
using SciFiLarpNetwork.Management.ViewModels.News;

namespace SciFiLarpNetwork.Management.Tests.ViewModels.News;

/// <summary>Opens the news screen in UTC at the fake API's fixed time.</summary>
internal static class NewsScreens
{
    /// <summary>A zone one hour ahead of UTC with no clock changes, so local times are easy to check.</summary>
    public static readonly TimeZoneInfo PlusOne = TimeZoneInfo.CreateCustomTimeZone("Test+1", TimeSpan.FromHours(1), "Test +1", "Test +1");

    public static async Task<NewsScreenViewModel> OpenAsync(
        FakeNewsApi api,
        FakeNewsDialogs? dialogs = null,
        string locale = "en",
        TimeZoneInfo? timeZone = null)
    {
        var screen = new NewsScreenViewModel(api, new JsonLocalizer(), dialogs ?? new FakeNewsDialogs(), "http://localhost/api", locale,
            FakeNewsApi.NewsHref, timeZone ?? TimeZoneInfo.Utc, new FixedTimeProvider(FakeNewsApi.Now));
        await screen.LoadAsync();
        return screen;
    }

    /// <summary>Selects a story and waits for its details, as clicking it in the list does.</summary>
    public static async Task SelectAsync(NewsScreenViewModel screen, string title)
    {
        screen.SelectedStory = screen.Stories.Single(story => story.DisplayName.Contains(title, StringComparison.Ordinal));
        await WaitUntilAsync(() => screen.DetailTitle == title);
    }

    public static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(5);
        }
    }
}
