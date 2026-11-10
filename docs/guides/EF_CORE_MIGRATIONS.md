# EF Core Migrations Workflow & Guide

This document defines the standard procedure for creating, reviewing, and applying Entity Framework Core migrations across SchoolOS modules.

## Architecture & Schema Boundaries

SchoolOS strictly separates the **Local Data Plane** and **Cloud Control Plane** persistence models into distinct DbContexts and logical PostgreSQL schemas:

| DbContext | Schema | Project Location | Startup Host |
|---|---|---|---|
| `OrganizationDbContext` | `organization` | `src/Modules/Organization/` | `SchoolOS.Local.Api` |
| `PlatformDbContext` | `platform` | `src/PlatformModules/Tenants/` | `SchoolOS.Cloud.Api` |

### Invariants:
1. **Never Mix Schemas:** Migrations for `OrganizationDbContext` must never reference or create tables in `platform`, and vice versa.
2. **Never Use `EnsureCreatedAsync` in Production:** Production environments must strictly apply versioned migrations using `Database.MigrateAsync()` or generated SQL migration bundles.
3. **Do Not Auto-Migrate on Application Startup in Production:** While local integration test fixtures invoke `MigrateAsync()` during test database setup, production application deployments execute migrations through a controlled CI/CD release gate or dedicated migration runner job.

---

## Tooling Setup

SchoolOS uses the local .NET tool manifest ([`dotnet-tools.json`](file:///d:/My%20Folder/My%20Work/SchoolOS/dotnet-tools.json)) pinning `dotnet-ef` to version `10.0.14`.

To restore tools on a new environment:
```bash
dotnet tool restore
```

Verify installation:
```bash
dotnet ef --version
```

---

## Migration Commands

### 1. Adding a Migration for Organization Module (Local Data Plane)

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Organization/SchoolOS.Modules.Organization.csproj \
  --startup-project src/Hosts/SchoolOS.Local.Api/SchoolOS.Local.Api.csproj \
  --context OrganizationDbContext \
  --output-dir Persistence/Migrations
```

### 2. Adding a Migration for Platform Tenants Module (Cloud Control Plane)

```bash
dotnet ef migrations add <MigrationName> \
  --project src/PlatformModules/Tenants/SchoolOS.PlatformModules.Tenants.csproj \
  --startup-project src/Hosts/SchoolOS.Cloud.Api/SchoolOS.Cloud.Api.csproj \
  --context PlatformDbContext \
  --output-dir Persistence/Migrations
```

---

## Design-Time DbContext Factories

Because `OrganizationDbContext` requires constructor injection of `ITenantContext` and runtime connection strings, EF Core tooling relies on design-time factories:
- [`OrganizationDbContextFactory`](file:///d:/My%20Folder/My%20Work/SchoolOS/src/Modules/Organization/Persistence/OrganizationDbContextFactory.cs)
- [`PlatformDbContextFactory`](file:///d:/My%20Folder/My%20Work/SchoolOS/src/PlatformModules/Tenants/Persistence/PlatformDbContextFactory.cs)

These factories configure the PostgreSQL provider with `.UseSnakeCaseNamingConvention()` and provide a dummy design-time tenant context solely for schema generation.

---

## Generating Idempotent SQL Scripts for Production

To generate an idempotent SQL script suitable for production deployment:

```bash
# Organization Module:
dotnet ef migrations script \
  --project src/Modules/Organization/SchoolOS.Modules.Organization.csproj \
  --startup-project src/Hosts/SchoolOS.Local.Api/SchoolOS.Local.Api.csproj \
  --context OrganizationDbContext \
  --idempotent \
  --output deploy/migrations/organization_migration.sql

# Platform Module:
dotnet ef migrations script \
  --project src/PlatformModules/Tenants/SchoolOS.PlatformModules.Tenants.csproj \
  --startup-project src/Hosts/SchoolOS.Cloud.Api/SchoolOS.Cloud.Api.csproj \
  --context PlatformDbContext \
  --idempotent \
  --output deploy/migrations/platform_migration.sql
```
