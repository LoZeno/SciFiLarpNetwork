namespace SciFiLarpNetwork.Backend.Domain.Networks;

/// <summary>
/// Product-defined network themes. A theme is presentation metadata only and never grants access.
/// </summary>
/// <remarks>
/// Members are persisted by name, so renaming a member is a breaking change to stored events.
/// </remarks>
public enum NetworkTheme
{
    Blue,
    Red,
    Green,
    Yellow,
    Amber,
    Grey,
    Purple
}
