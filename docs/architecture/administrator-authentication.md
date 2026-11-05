# Local administrator authentication

Administrator accounts are local application identities, separate from Player, Character, and in-game network identities. No account is created by default, and there is no public bootstrap or recovery endpoint. Protect access to the backend host, its interactive console, and its PostgreSQL credentials: a trusted operator with that access can bootstrap or recover an account. Normal authentication and authorization require no external identity provider or internet access.

## First administrator

Start PostgreSQL and configure the backend connection for the intended environment. From an interactive terminal on the backend host, run the bootstrap command once. For the local Compose development database, from the repository root in PowerShell:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project Backend/src/Host/SciFiLarpNetwork.Backend.csproj -- --bootstrap-admin
```

On Linux or macOS:

```sh
DOTNET_ENVIRONMENT=Development dotnet run --project Backend/src/Host/SciFiLarpNetwork.Backend.csproj -- --bootstrap-admin
```

Enter the account name and password when prompted; password input is hidden and must be confirmed. Account names are 3–64 characters, start with a letter or digit, and may contain letters, digits, spaces, `.`, `_`, `@`, `+`, and `-`. Passwords must contain 12–128 characters and cannot be all whitespace. The command creates the first `administrator` only if no administrator accounts exist; concurrent attempts cannot create multiple first accounts. It generates and consumes a one-use grant in the process: only its hash is stored, it expires after 15 minutes, and the secret is not printed or logged. Do not pass passwords or grants as command-line arguments.

For a deployed framework-dependent host, run `dotnet SciFiLarpNetwork.Backend.dll --bootstrap-admin` from the directory containing the host's settings files, with the production connection configured. Use an actual interactive terminal; redirected input is rejected. Configure production credentials separately and do not reuse the development database credentials.

## Recovery and account roles

If an account's credentials are lost, a trusted local operator can run the same command with `--recover-admin` instead of `--bootstrap-admin`. Enter the existing account name and a new confirmed password. Recovery uses a new one-use, 15-minute grant, preserves the account's role, and revokes all of that account's existing sessions. It does not create an account or elevate a reader to administrator.

The `reader` role may query administrator data; `administrator` may also create administrator accounts and change their roles through the protected API. A reader cannot grant itself access, and the final administrator cannot be demoted. Account creation, role changes, and recovery are recorded durably with their authenticated administrator or local-operator actor; HTTP-originated domain-event appends are attributed to the authenticated account rather than an actor supplied by the client. The management shell currently provides sign-in and protected navigation, not account-creation or role-editing forms.

Password verifiers use ASP.NET Core Identity's salted password hasher. Passwords and reusable session or operator secrets are not stored in event payloads, settings, URLs, or logs. Administrator accounts are not automatically linked to a Player or Character with a matching name.

## Sessions and transport

The management app signs in with the local backend and keeps the bearer token only in memory, bound to the API origin and path that issued it. After signing in, select **Connect** to refresh the authenticated Siren navigation and open the administrator directory. Logout clears local session state and asks the backend to revoke the token. If the backend cannot be reached during logout, the client still clears its copy, but the server-side token remains valid until expiry or credential recovery.

Sessions expire eight hours after issuance. The API supports renewal, which rotates the token and gives the new token another eight hours; the old token immediately stops working. The current desktop UI does not automatically renew, so sign in again after expiry. Credential recovery revokes all sessions for the recovered account. Unknown, expired, revoked, or insufficiently authorized sessions receive stable Siren errors. Sessions are not cookie-based, so cookie CSRF protection is not involved.

The sign-in endpoint is `POST /api/v1/admin/session`; a successful Siren body contains account and expiry information, while the bearer token is returned in the `Authorization` response header, not in the representation or URL. `POST /api/v1/admin/session/renew` rotates it and `DELETE /api/v1/admin/session` revokes it. Administrator queries and writes require the bearer header and the appropriate server-side role. Do not record request/response authorization headers or credentials in proxy, diagnostic, or terminal logs.

HTTP is permitted only for loopback development. Remote administrator traffic requires HTTPS, and the desktop client refuses to send sign-in credentials over remote HTTP. The current host does not configure trusted forwarded headers for a TLS-terminating proxy; use an HTTPS connection recognized by the backend, and configure any future proxy trust explicitly before relying on it. Restrict network exposure and PostgreSQL access to trusted operators.