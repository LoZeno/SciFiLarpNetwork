namespace SciFiLarpNetwork.Management.ViewModels;

/// <param name="OpensTalentCatalog">The link carries the talents relation, so it opens the talents and skills screen.</param>
public sealed record NavigationItem(string Title, string Href, bool OpensTalentCatalog = false);
