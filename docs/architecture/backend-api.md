# Backend API baseline

The backend is the .NET 10 ASP.NET Core MVC application in `Backend/src/Host`. It exposes API discovery, OpenAPI, durable event-store infrastructure, and local administrator authentication; feature-specific domain workflows follow the SPEC-22 order.

## Run and discover

From the repository root, start the local PostgreSQL service first. The same Compose file and commands work on Windows, Linux, and macOS with Docker Desktop or Docker Engine plus the Compose plugin:

```text
docker compose up -d
docker compose ps
```

Wait for the `postgres` service to report `healthy`, then run the backend in Development. In PowerShell:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project Backend/src/Host/SciFiLarpNetwork.Backend.csproj --urls http://localhost:5080
```

On Linux or macOS shells:

```sh
DOTNET_ENVIRONMENT=Development dotnet run --project Backend/src/Host/SciFiLarpNetwork.Backend.csproj --urls http://localhost:5080
```

- `GET /api` is the Siren entry point and links to the latest version and OpenAPI description.
- `GET /api/v1` is the initial versioned entry point.
- `GET /openapi/v1.json` returns the OpenAPI document as `application/json`.
- Successful Siren resources and API errors use `application/vnd.siren+json`. Clients should send that media type in `Accept`.
- `Accept-Language` selects English (`en`) or Italian (`it`); unsupported or missing languages fall back to English.
- The [Siren API profile](siren-api-profile.md) defines relations, classes, actions, localization, and error contracts.

## Configuration and network exposure

The host loads `appsettings.json` and the selected `appsettings.{Environment}.json` file. `Development` uses the local PostgreSQL connection string in `Backend/src/Host/appsettings.Development.json`; the Compose service creates the `scifilarnetwork_dev` database and persists its data in the `postgres_data` named volume. The development credentials are local-only defaults, not production credentials.

For `Production`, `appsettings.Production.json` intentionally contains an empty `ConnectionStrings:EventStore`; the host fails startup until an operator supplies the production connection string. Set `ConnectionStrings__EventStore` in the process environment (preferably injected by the deployment's secret-management mechanism), or use another higher-priority ASP.NET Core configuration provider. Environment variables override the JSON files; for example, `ConnectionStrings__EventStore` maps to `ConnectionStrings:EventStore`. Do not put production credentials in committed settings files.

`DOTNET_ENVIRONMENT` selects the environment-specific file and takes precedence over `ASPNETCORE_ENVIRONMENT`; if neither is set, the environment defaults to `Production`. `ASPNETCORE_URLS` or the `--urls` argument configures Kestrel's bind URLs. Use loopback for local development; exposing the service to a venue LAN requires an explicit LAN bind address and appropriate host firewall rules.

The PostgreSQL image supports both x64 and ARM64; Compose leaves platform selection unspecified so Docker uses the image matching the host architecture, including Windows x64/ARM64, Linux, and Intel/Apple Silicon Macs. The service binds only to loopback port `5432`; change the host port in `compose.yaml` if another local PostgreSQL instance already uses it, and update the application connection string accordingly.

Stop PostgreSQL while keeping its data with `docker compose down`. The test database is created by the initialization SQL only when PostgreSQL first initializes an empty volume. If you already have a volume from before that script was added, create `scifilarnetwork_tests` manually or, only if you are certain all local database data can be discarded, reset the service with `docker compose down -v` followed by `docker compose up -d`. Removing the volume permanently deletes both local databases. This Compose setup is for local development and tests only, not production.

The optional `PathBase` configuration value mounts the application below a path prefix (for example, `/network`). It must be an absolute path without a trailing slash, query, or fragment; generated Siren and OpenAPI links preserve it. The API and local resources have no runtime internet dependency. OpenAPI generation, localization resources, and Siren serialization are included with the application. See [local administrator authentication](administrator-authentication.md) for bootstrap, recovery, roles, sessions, and HTTPS requirements. Player sign-in and feature-specific endpoints remain later work.
