# Backend domain-driven design structure

The backend is split into projects by responsibility so domain behavior remains independent of transport and persistence details.

| Project | Responsibility | May depend on |
|---|---|---|
| `Backend/src/Domain` (`SciFiLarpNetwork.Backend.Domain.csproj`) | Domain concepts, invariants, domain events, and deterministic event evolution/replay | .NET base libraries |
| `Backend/src/Application` (`SciFiLarpNetwork.Backend.Application.csproj`) | Use cases, command/query contracts, and application ports such as `IEventActorContext` | Domain |
| `Backend/src/Infrastructure` (`SciFiLarpNetwork.Backend.Infrastructure.csproj`) | Marten/PostgreSQL adapters, schema lifecycle, projection runtime, health diagnostics, and dependency-injection registration | Application, Domain, provider/framework packages |
| `Backend/src/Host` (`SciFiLarpNetwork.Backend.csproj`) | ASP.NET Core MVC/API presentation and composition root | Application, Infrastructure, shared API/localization contracts |

Dependency direction is inward: Domain has no reference to Application, Infrastructure, Marten, ASP.NET Core, or the database. Application depends on Domain and defines persistence-facing interfaces; Infrastructure implements those interfaces. The HTTP host translates requests and responses and performs composition, but does not own domain decisions or access Marten sessions directly.

As product capabilities are implemented, place each bounded context's aggregates, value objects, and domain events under a named Domain feature area; place its application commands, handlers, and query contracts under the corresponding Application feature area; and place provider-specific persistence/projection adapters under Infrastructure. Keep MVC controllers thin. Do not introduce placeholder aggregates or make generic infrastructure the owner of feature invariants before the relevant feature specification.

The initial event-store runtime follows these boundaries: immutable event contracts are in Domain, the actor-context port is in Application, Marten configuration, command handlers that use Marten sessions, projections, and database lifecycle are in Infrastructure, and the ASP.NET Core host only registers the Infrastructure composition extension. There is no provider-neutral event-store port; see [SPEC-24](../specifications/SPEC-24-event-store-runtime.md).
