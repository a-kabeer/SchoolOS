# 19 — Technical References

The following official documentation supports selected implementation constraints. Verify the current page and supported package versions again when implementation begins.

## .NET lifecycle

- Microsoft .NET and .NET Core lifecycle: https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core
- .NET releases and support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

As checked on 9 November 2026, Microsoft lists .NET 10 support through 15 November 2028. Select supported patches and keep the runtime updated.

## ASP.NET Core Data Protection

- Configure ASP.NET Core Data Protection: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview

For container deployments, persist the Data Protection key ring in a persistent Docker/host volume or an appropriate external provider. Protect the key repository with restrictive permissions and an at-rest protection strategy. Do not leave keys only in an ephemeral container filesystem.

## PostgreSQL Row-Level Security

- PostgreSQL Row Security Policies: https://www.postgresql.org/docs/current/ddl-rowsecurity.html

PostgreSQL documents that superusers and roles with `BYPASSRLS` bypass row security and that table owners normally bypass it unless row security is forced. RLS must therefore be combined with restricted runtime roles and automated isolation tests.

## ASP.NET Core background work

- Background tasks with hosted services: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services

Use hosted services/Worker processes for durable sync, update checks and other background work only with a persistent queue/state model. Do not rely on an in-memory queue for business events that must survive process restart.

## Design note

The references explain framework/platform behaviour; they do not validate SchoolOS's complete architecture, license protocol, cryptographic implementation or security posture. Those require code review, threat modelling and testing.

## Additional internal design references

Campus data exchange and multi-campus deployment profiles are specified in `docs/18_CAMPUS_DATA_ACCESS_AND_DEPLOYMENT_SCENARIOS.md` and ADR-014. This is a product/architecture design, not a claim that manual exchange has already been implemented or independently tested.
