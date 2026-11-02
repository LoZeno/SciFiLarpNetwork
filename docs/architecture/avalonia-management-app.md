# Avalonia management application baseline

The desktop administration client is a .NET 10 Avalonia application in `Management/src`. It is an API client, not a database or event-store editor. It discovers the API version and advertised navigation through the Siren profile and follows only links within the configured `/api` path and origin. Advertised actions are submitted only explicitly, and the initial client supports JSON request bodies.

Run the app from the repository root with:

```powershell
dotnet run --project Management/src/SciFiLarpNetwork.Management.csproj
```

The default API address is `http://localhost:5000/api`. Operators can change it in the connection panel. English is selected by default; English/Italian preference and the last API address are stored in the user's application-data directory under `SciFiLarpNetwork/management-settings.json`. If the settings file cannot be read, the client uses defaults and shows a localized warning.

The current backend exposes only API discovery. The client therefore provides the desktop shell, connection status, advertised navigation, resource summary, and advertised-action list; feature-specific administration forms are delivered with their corresponding manager specifications. UI assets and localization resources are local. Runtime access is limited to the configured API; no web server, direct database connection, external identity provider, or remote content is used by this desktop client.

Building and launching this baseline on the Windows ARM64 development host does not establish support for all six approved runtime identifiers. Per-target build, packaging, and launch evidence remains a release gate under SPEC-07 and the later CI/deployment specifications.
