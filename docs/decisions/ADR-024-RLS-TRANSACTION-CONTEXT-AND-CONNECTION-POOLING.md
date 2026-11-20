# ADR-024 — PostgreSQL RLS Transaction Context and Connection Pooling Isolation

- **Document ID:** ADR-024
- **Status:** Accepted (Phase 1A.1 Stabilization Baseline)
- **Decision maturity:** Accepted architecture decision (Phase 1A.1 baseline)
- **Owner:** Software Architecture Team
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `integrate/phase-1-and-education-portal`
- **Date:** 2026-11-10
- **Reviewers:** Technical Lead, Security Engineer, Database Architect

## Context

SchoolOS utilizes PostgreSQL 17 Row-Level Security (RLS) to enforce tenant data isolation at the database engine level (`docs/04_DATABASE_BLUEPRINT.md`). RLS policies filter rows by evaluating:
```sql
USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
```

In high-throughput ASP.NET Core environments, Npgsql maintains a pool of physical database connections. When physical connections are reused across different HTTP requests:
1. If a session configuration parameter is set via `set_config('app.current_tenant_id', @id, false)`, it applies to the entire physical session.
2. If connection pooling does not reset state between check-in and check-out, a subsequent request borrowing that physical connection could inadvertently inherit the previous tenant's identity if an unauthenticated or error state occurs before re-initialization.

This ADR defines the architectural strategy ensuring zero tenant context leakage across pooled connections.

## Decision Drivers

1. **Zero Context Leakage:** A connection returned to the pool must never expose the preceding tenant's context to any subsequent caller.
2. **PostgreSQL Engine Guarantees:** Rely on database engine-level scoping rather than solely application-level memory discipline.
3. **Fail-Closed by Default:** Any query executed without an explicit, authenticated tenant identifier must return zero protected rows.
4. **Performance & Overhead:** Avoid issuing costly full connection resets (`DISCARD ALL`) on every trivial query if lighter, transaction-local mechanisms provide equivalent security.

## Decision

We adopt a **Dual-Tier Transaction-Local and Fail-Closed Session Reset Strategy**:

### 1. Transaction-Local Tenant Scoping (`PostgresTenantTransactionInterceptor`)
For all transactional operations, EF Core executes:
```sql
SELECT set_config('app.current_tenant_id', @tenant_id, true);
```
- By setting the third argument `is_local = true`, PostgreSQL binds the configuration parameter strictly to the active transaction.
- When the transaction ends via `COMMIT`, `ROLLBACK`, or unhandled exception abort, the PostgreSQL database engine automatically wipes the variable without requiring client intervention.

### 2. Baseline Connection Initialization & Explicit Pool Return Reset (`PostgresTenantSessionInterceptor`)
To protect non-transactional read queries and eliminate residual session artifacts:
- **On Connection Opened (Non-Transactional Baseline):** The interceptor immediately executes `SELECT set_config('app.current_tenant_id', @tenant_id, false);`. If `ITenantContext.TenantId` is null, it explicitly sets the empty string `''`. This ensures non-transactional read queries (e.g., `await db.Schools.ToListAsync()`) execute within the correct tenant scope without requiring an explicit transaction boundary.
- **Coordination with Transactional Operations:** When a transaction is started, `PostgresTenantTransactionInterceptor` executes with `is_local = true`. This overrides the session setting for the duration of the transaction. Upon commit or rollback, PostgreSQL automatically reverts the parameter to its pre-transaction state.
- **On Connection Closing (Pool Check-In & Zero-Swallow Policy):** Before Npgsql returns the connection to the connection pool, `ConnectionClosingAsync` intercepts the lifecycle event and executes:
  ```sql
  SELECT set_config('app.current_tenant_id', '', false);
  ```
  This guarantees that any idle connection sitting in the pool has `app.current_tenant_id = ''`.
- **Pool-Clearing on Reset Failure (`NpgsqlConnection.ClearPool`):** In ADO.NET and Npgsql with connection pooling enabled, calling `Close()` or `Dispose()` on a connection merely returns the physical connection to the pool for reuse; it does not terminate or destroy the underlying physical socket. Therefore, if the session reset query (`SELECT set_config('app.current_tenant_id', '', false);`) fails, relying on `Close()` alone would return a contaminated, uncleared connection to the pool where a subsequent request could lease it. To prevent this, the interceptor invokes `NpgsqlConnection.ClearPool(npgsqlConnection)`. According to Npgsql connection pooling mechanics:
  1. `ClearPool` immediately closes and disposes all idle physical connections currently sitting in that connection pool.
  2. For busy connections (connections currently in use, including the active connection being returned), `ClearPool` marks them so that when they are returned to the cleared pool (via `Close()` or `Dispose()`), Npgsql closes and terminates the physical connection instead of keeping it in the pool for reuse.
  3. The interceptor preserves the original exception that caused the reset failure, records any auxiliary failure from `ClearPool`, avoids recursive `Close()` calls within `ConnectionClosing`/`ConnectionClosingAsync`, and raises an `InvalidOperationException` (or `AggregateException`) to ensure operations fail closed.

### 3. Fail-Closed SQL Evaluation
The RLS policy is formulated as:
```sql
CREATE POLICY {table}_tenant_isolation ON {schema}.{table}
    FOR ALL
    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
```
- If `app.current_tenant_id` is empty string or unset, `NULLIF(..., '')` produces SQL `NULL`.
- Because `tenant_id` columns are defined as `NOT NULL`, `tenant_id = NULL` evaluates to `UNKNOWN` (falsy in SQL predicates), returning zero rows.

### 4. Non-Superuser Role Enforcement & Least-Privilege Runtime Grants
By PostgreSQL engine design, superusers and roles with `BYPASSRLS` bypass row-level security.
- Schema migrations and DDL operations execute under the administrative owner role `schoolos_dev`.
- Application and integration test connections execute under the non-superuser role `schoolos_app` (`NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS`), ensuring that RLS policies are strictly enforced by the PostgreSQL query planner.
- `schoolos_app` is granted ONLY `USAGE` on target schemas, `SELECT, INSERT, UPDATE, DELETE` on tables, and `USAGE, SELECT` on sequences (`PostgresRlsHelper.GenerateLeastPrivilegeTenantRoleGrantsSql`). It MUST NOT own protected tables and MUST NOT be granted `CREATE`, `TRUNCATE`, `TRIGGER`, or `REFERENCES`.
- Every protected table MUST enable both `ENABLE ROW LEVEL SECURITY` and `FORCE ROW LEVEL SECURITY` directly inside its EF Core migration (`Up`/`Down`), not only in test harnesses.

### 5. Prohibition of Custom GUC Bypasses & Secure Platform-Wide Access Boundary
Custom placeholder PostgreSQL GUCs (such as `app.is_platform_admin`) MUST NEVER be used as an RLS bypass predicate:
- In PostgreSQL, unregistered two-part custom GUCs (`app.*`) are `PGC_USERSET` and can be set via `SELECT set_config('app.is_platform_admin', 'true', ...)` by **any** connected role, including a `NOSUPERUSER NOBYPASSRLS` tenant runtime role (`schoolos_app`).
- `NOBYPASSRLS` prevents a role from skipping RLS evaluation; it does **not** prevent that role from mutating custom GUCs to satisfy a policy expression.
- Any future Cloud Control Plane table that legitimately requires platform-wide cross-tenant access (never Local Data Plane operational tables such as `organization.*`) MUST instead authorize via an independently credentialed PostgreSQL role (`schoolos_platform_admin`, `NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS`) with:
  1. No role membership granted to `schoolos_app` (so `SET ROLE schoolos_platform_admin` is denied with SQLSTATE `42501`).
  2. Explicit per-table role-scoped policy (`CREATE POLICY ... FOR ALL TO schoolos_platform_admin USING (true) WITH CHECK (true)` via `PostgresRlsHelper.GeneratePlatformAdminRolePolicySql`), never `TO PUBLIC` and never `TO schoolos_app`.
  3. Separate connection string / secret boundary in the Cloud host or lifecycle job.
- `SchoolOSDbContextBase.ValidateTenantIntegrity()` fails closed unconditionally whenever an `ITenantScopedEntity` is mutated with `TenantContext.TenantId == null`, regardless of `IsPlatformAdmin`.

## Consequences

### Positive
- Prevents cross-tenant context leakage across pooled Npgsql connections.
- Transaction aborts and rollbacks cleanly discard tenant parameters at the engine level.
- Multi-layered defense: works in tandem with EF Core global query filters.

### Negative / Trade-offs
- Setting session parameters introduces an additional SQL execution upon connection checkout and transaction start. In local LAN and cloud environments with pipelined connection pooling, this overhead is under 0.5ms per transaction.

## References
- `docs/04_DATABASE_BLUEPRINT.md`
- `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md`
- PostgreSQL 17 Documentation: `set_config`, Row-Level Security
