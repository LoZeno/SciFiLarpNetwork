namespace SciFiLarpNetwork.Backend.Application.News;

public enum NewsOutcome
{
    Succeeded,
    NotFound,

    /// <summary>The client must give the story an identifier that is not empty.</summary>
    InvalidStoryId,

    InvalidTitle,
    InvalidBody,

    /// <summary>The end is not after the start.</summary>
    InvalidTimes,

    /// <summary>The hacking difficulty is below 1.</summary>
    InvalidHackingDifficulty,

    UnknownNetwork,
    NetworkDisabled,
    UnknownAuthor,
    AuthorArchived,

    /// <summary>An image of the body points outside the media library.</summary>
    ExternalImage,

    /// <summary>A picture or video of the body has no text for people who cannot see it.</summary>
    MissingMediaText,

    /// <summary>A <c>media:</c> address of the body is malformed, or has an unknown or repeated option.</summary>
    InvalidMediaReference,

    /// <summary>The body places more than 10 pictures and videos.</summary>
    TooManyMedia,

    /// <summary>The body names media that is not in the library.</summary>
    UnknownMedia,

    /// <summary>The body gives video options to a picture.</summary>
    OptionsOnPicture,

    /// <summary>The identifier already names a different story, or another stream.</summary>
    StoryIdInUse,

    /// <summary>The story or media changed since it was read.</summary>
    ConcurrentChange,

    InvalidMediaName,

    /// <summary>The file is not a PNG, JPEG, WebP, or GIF picture, or a WebM or MP4 video.</summary>
    InvalidMediaFile,

    MediaTooLarge,
    MediaDimensionsTooLarge,

    /// <summary>A story's current body uses the media.</summary>
    MediaInUse
}
