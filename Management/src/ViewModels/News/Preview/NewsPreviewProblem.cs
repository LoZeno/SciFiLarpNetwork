namespace SciFiLarpNetwork.Management.ViewModels.News.Preview;

/// <summary>An image the story cannot show: from outside the library, or no longer in it.</summary>
/// <param name="Message">What is wrong, in the user's language.</param>
public sealed record NewsPreviewProblem(string Message, int Indent) : NewsPreviewBlock(Indent);
