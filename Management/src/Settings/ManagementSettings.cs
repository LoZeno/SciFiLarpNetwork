namespace SciFiLarpNetwork.Management.Settings;

public sealed record ManagementSettings(
    string ApiBaseUrl = "http://localhost:5000/api",
    string Locale = "en");
