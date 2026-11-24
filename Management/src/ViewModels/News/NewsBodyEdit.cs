namespace SciFiLarpNetwork.Management.ViewModels.News;

/// <summary>A body text with the selection to show after a toolbar change.</summary>
public sealed record NewsBodyEdit(string Text, int SelectionStart, int SelectionEnd);
