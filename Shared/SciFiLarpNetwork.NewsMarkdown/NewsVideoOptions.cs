namespace SciFiLarpNetwork.Common.NewsMarkdown;

/// <summary>How an embedded video plays. An autoplaying video always starts muted.</summary>
[Flags]
public enum NewsVideoOptions
{
    None = 0,
    Autoplay = 1,
    Loop = 2,
    Controls = 4
}
