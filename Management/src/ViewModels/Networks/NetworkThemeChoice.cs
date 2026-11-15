namespace SciFiLarpNetwork.Management.ViewModels.Networks;

/// <param name="Name">The theme's stable name, sent to the API.</param>
/// <param name="Title">The theme's localized title, from the API.</param>
public sealed record NetworkThemeChoice(string Name, string Title);
