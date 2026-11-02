# Backend API baseline

The backend is the .NET 10 ASP.NET Core MVC application in `Backend/src`. It currently exposes the API discovery roots and the OpenAPI document; domain/authentication workflows are added only in their SPEC-22 order.

## Run and discover

From the repository root:

```powershell
dotnet run --project Backend/src/SciFiLarpNetwork.Backend.csproj --urls http://localhost:5080
```

- `GET /api` is the Siren entry point and links to the latest version and OpenAPI description.
- `GET /api/v1` is the initial versioned entry point.
- `GET /openapi/v1.json` returns the OpenAPI document as `application/json`.
- Successful Siren resources and API errors use `application/vnd.siren+json`. Clients should send that media type in `Accept`.
- `Accept-Language` selects English (`en`) or Italian (`it`); unsupported or missing languages fall back to English.
- The [Siren API profile](siren-api-profile.md) defines relations, classes, actions, localization, and error contracts.

## Configuration and network exposure

ASP.NET Core's standard configuration providers apply: environment variables, command-line arguments, and deployment-specific configuration. `ASPNETCORE_URLS` or the `--urls` argument configures Kestrel's bind URLs. Use loopback for local development; exposing the service to a venue LAN requires an explicit LAN bind address and appropriate host firewall rules.

The optional `PathBase` configuration value mounts the application below a path prefix (for example, `/network`). It must be an absolute path without a trailing slash, query, or fragment; generated Siren and OpenAPI links preserve it. Do not commit machine-specific URLs, credentials, or secrets. No application credentials or database connection strings are required by this API-discovery baseline; those are configured securely when their features are introduced.

The API and local resources have no runtime internet dependency. OpenAPI generation, localization resources, and Siren serialization are included with the application. This baseline does not yet implement feature endpoints, account sign-in, session issuance, or domain persistence.
