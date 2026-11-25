namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>One drawn part of a story preview.</summary>
/// <param name="Indent">How many list or quote levels the block sits in.</param>
public abstract record NewsPreviewBlock(int Indent);
