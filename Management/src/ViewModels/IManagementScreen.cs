namespace SciFiLarpNetwork.Management.ViewModels;

/// <summary>A purpose-built screen shown in place of the resource summary.</summary>
public interface IManagementScreen
{
    /// <summary>Raised when the API rejects the session, so the shell can clear its signed-in state.</summary>
    event EventHandler? SessionExpired;

    string Locale { get; set; }

    /// <summary>Whether the screen's first load succeeded.</summary>
    bool IsLoaded { get; }

    Task LoadAsync();
}
