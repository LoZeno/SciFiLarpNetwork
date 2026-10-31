# SPEC-05: ASP.NET Core MVC backend

## Requirement

Implement the backend as an ASP.NET Core MVC application targeting .NET 10. MVC controllers expose HTTP endpoints for the SPA and administration client. Domain behavior and persistence concerns must remain separated from transport and presentation concerns.

## Acceptance criteria

- The backend uses ASP.NET Core MVC controllers and standard dependency injection/configuration/hosting facilities.
- HTTP endpoints use typed request/response contracts, appropriate status codes, input validation, and consistent error responses.
- API documentation is available to developers and can be used to verify client/server contract compatibility.
- Environment-specific settings and secrets are supplied through configuration or secure deployment mechanisms, not machine-specific source-controlled paths.
