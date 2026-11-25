namespace SciFiLarpNetwork.Common.NewsMarkdown;

public enum NewsBodyMediaProblem
{
    /// <summary>An image points somewhere other than the media library.</summary>
    ExternalImage,

    /// <summary>A picture or video has no text for people who cannot see it.</summary>
    MissingText,

    /// <summary>A <c>media:</c> address has no valid identifier, or an unknown or repeated option.</summary>
    InvalidReference
}
