namespace SciFiLarpNetwork.Management.ViewModels;

public sealed record NavigationItem(string Title, string Href, NavigationScreen Screen = NavigationScreen.ResourceSummary);
