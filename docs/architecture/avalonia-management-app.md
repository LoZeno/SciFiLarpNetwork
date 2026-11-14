# Avalonia management application baseline

The desktop administration client is a .NET 10 Avalonia application in `Management/src`. It is an API client, not a database or event-store editor. It discovers the API version and advertised navigation through the Siren profile and follows only links within the configured `/api` path and origin. Advertised actions are submitted only explicitly, and the initial client supports JSON request bodies.

Run the app from the repository root with:

```powershell
dotnet run --project Management/src/SciFiLarpNetwork.Management.csproj
```

The app reads and saves `appsettings.json` in the same directory as its executable. The shipped defaults use API address `http://localhost:5000/api` and locale `en`; the connection panel saves the last API address and selected English/Italian locale back to that file. Keep a backup of local edits before rebuilding or redeploying, as publishing or replacing the application can replace the settings file. The executable directory must be writable for changes to persist; if the file is missing, unreadable, malformed, or cannot be saved, the client falls back to defaults where applicable and shows a localized warning.

Existing preferences in `%APPDATA%/SciFiLarpNetwork/management-settings.json` are not imported automatically. To keep them, copy the `apiBaseUrl` and `locale` values into the `appsettings.json` beside the executable before launching the new version.

After a local operator bootstraps an administrator, enter the account name and password in the connection panel, select **Sign in**, then **Connect** to refresh authenticated navigation. Selecting a navigation entry opens it immediately. The client holds the session token only in memory, sends it only to its issuing API, and clears it and protected navigation on logout or an expired-session response. Remote administrator connections require HTTPS. The [administrator guide](administrator-authentication.md) describes bootstrap, recovery, session expiry, and roles.

The client provides the desktop shell, connection status, advertised navigation, resource summary, and advertised-action list. Navigation entries without a purpose-built screen show the resource summary.

The **Talents** entry opens the talents and skills screen ([MANAGER-02](../specifications/legacy-features/manager-tabs/MANAGER-02-skills.md)). It lists talents on the left and the skills of the selected talent on the right; each list has a **Show archived** option, and archived entries are marked. Administrators get **New**, **Edit**, and **Archive**/**Unarchive** buttons for both lists; readers only browse, because the API advertises no change actions to them. Talents and skills are edited in modal dialogs. The skill dialog covers cost, flags, talent assignment (archived talents appear only when already assigned), game flags, existing and newly typed card flags (one name per line), and prerequisite groups: the character needs every group, and any one skill in each group. Archived prerequisites are listed in a warning but can still be saved. Archiving and unarchiving ask for confirmation. Validation and server errors appear inside the dialog, which stays open; a stale edit shows the conflict and reloads the latest values. The screen follows only advertised links and actions and never builds API addresses itself.

Account-creation, role-editing, and the other feature-specific administration screens are not yet provided. UI assets and localization resources are local. Runtime access is limited to the configured API; no web server, direct database connection, external identity provider, or remote content is used by this desktop client.

Building and launching this baseline on the Windows ARM64 development host does not establish support for all six approved runtime identifiers. Per-target build, packaging, and launch evidence remains a release gate under SPEC-07 and the later CI/deployment specifications.
