namespace SciFiLarpNetwork.Management.Api;

public sealed record ManagementSession(string UserName, string Role, DateTimeOffset ExpiresAtUtc);
