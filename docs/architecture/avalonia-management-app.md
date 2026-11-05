# Avalonia management application baseline

The desktop administration client is a .NET 10 Avalonia application in `Management/src`. It is an API client, not a database or event-store editor. It discovers the API version and advertised navigation through the Siren profile and follows only links within the configured `/api` path and origin. Advertised actions are submitted only explicitly, and the initial client supports JSON request bodies.

Run the app from the repository root with:

```powershell
dotnet run --project Management/src/SciFiLarpNetwork.Management.csproj
```

The app reads and saves `appsettings.json` in the same directory as its executable. The shipped defaults use API address `http://localhost:5000/api` and locale `en`; the connection panel saves the last API address and selected English/Italian locale back to that file. Keep a backup of local edits before rebuilding or redeploying, as publishing or replacing the application can replace the settings file. The executable directory must be writable for changes to persist; if the file is missing, unreadable, malformed, or cannot be saved, the client falls back to defaults where applicable and shows a localized warning.

Existing preferences in `%APPDATA%/SciFiLarpNetwork/management-settings.json` are not imported automatically. To keep them, copy the `apiBaseUrl` and `locale` values into the `appsettings.json` beside the executable before launching the new version.

The current backend exposes only API discovery. The client therefore provides the desktop shell, connection status, advertised navigation, resource summary, and advertised-action list; feature-specific administration forms are delivered with their corresponding manager specifications. UI assets and localization resources are local. Runtime access is limited to the configured API; no web server, direct database connection, external identity provider, or remote content is used by this desktop client.

Building and launching this baseline on the Windows ARM64 development host does not establish support for all six approved runtime identifiers. Per-target build, packaging, and launch evidence remains a release gate under SPEC-07 and the later CI/deployment specifications.
