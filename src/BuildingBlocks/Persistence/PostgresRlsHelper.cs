using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace SchoolOS.BuildingBlocks.Persistence;

/// <summary>
/// Utility for generating and applying PostgreSQL Row-Level Security (RLS) policies
/// and least-privilege runtime role grants.
/// </summary>
public static partial class PostgresRlsHelper
{
    private static readonly HashSet<string> DisallowedPlatformAdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "public",
        "current_user",
        "session_user",
        "current_role",
        "schoolos_app"
    };

    [GeneratedRegex("^[a-z_][a-z0-9_]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafePostgresIdentifierRegex();

    /// <summary>
    /// Generates idempotent SQL commands to enable and force strict tenant isolation via PostgreSQL RLS.
    /// Never uses custom GUC bypass flags (such as <c>app.is_platform_admin</c>), because any PostgreSQL role
    /// — including a <c>NOSUPERUSER NOBYPASSRLS</c> tenant runtime role — can call <c>set_config</c> on custom GUCs.
    /// </summary>
    public static string GenerateEnableRlsSql(
        string schema,
        string tableName,
        string tenantIdColumn = "tenant_id")
    {
        ValidateIdentifier(schema, nameof(schema));
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(tenantIdColumn, nameof(tenantIdColumn));

        var policyName = $"{tableName}_tenant_isolation";
        var predicate = $"({tenantIdColumn} = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)";

        return $"""
            ALTER TABLE {schema}.{tableName} ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {schema}.{tableName} FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS {policyName} ON {schema}.{tableName};
            CREATE POLICY {policyName} ON {schema}.{tableName}
                FOR ALL
                USING {predicate}
                WITH CHECK {predicate};
            """;
    }

    /// <summary>
    /// Generates a PostgreSQL role-scoped RLS policy granting platform-wide cross-tenant access
    /// ONLY to an independently authenticated, <c>NOINHERIT NOSUPERUSER NOBYPASSRLS</c> platform-admin database role.
    /// <para>
    /// Security boundary: apply ONLY to explicitly allowlisted Cloud Control Plane tables.
    /// NEVER apply to Local Data Plane operational tables (e.g. <c>organization.*</c>), and NEVER grant
    /// <paramref name="platformAdminRoleName"/> membership to the tenant runtime role (<c>schoolos_app</c>).
    /// </para>
    /// </summary>
    public static string GeneratePlatformAdminRolePolicySql(
        string schema,
        string tableName,
        string platformAdminRoleName)
    {
        ValidateIdentifier(schema, nameof(schema));
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(platformAdminRoleName, nameof(platformAdminRoleName));

        if (DisallowedPlatformAdminRoles.Contains(platformAdminRoleName))
        {
            throw new ArgumentException(
                $"Role '{platformAdminRoleName}' cannot be granted platform-wide RLS access.",
                nameof(platformAdminRoleName));
        }

        var policyName = $"{tableName}_platform_admin_access";

        return $"""
            DROP POLICY IF EXISTS {policyName} ON {schema}.{tableName};
            CREATE POLICY {policyName} ON {schema}.{tableName}
                FOR ALL
                TO {platformAdminRoleName}
                USING (true)
                WITH CHECK (true);
            """;
    }

    /// <summary>
    /// Generates idempotent SQL to drop a role-scoped platform-admin RLS policy.
    /// </summary>
    public static string GenerateDropPlatformAdminRolePolicySql(string schema, string tableName)
    {
        ValidateIdentifier(schema, nameof(schema));
        ValidateIdentifier(tableName, nameof(tableName));

        var policyName = $"{tableName}_platform_admin_access";
        return $"DROP POLICY IF EXISTS {policyName} ON {schema}.{tableName};";
    }

    /// <summary>
    /// Generates idempotent SQL commands to drop the tenant isolation RLS policy and disable RLS during migration rollback.
    /// </summary>
    public static string GenerateDisableRlsSql(string schema, string tableName)
    {
        ValidateIdentifier(schema, nameof(schema));
        ValidateIdentifier(tableName, nameof(tableName));

        var policyName = $"{tableName}_tenant_isolation";
        return $"""
            DROP POLICY IF EXISTS {policyName} ON {schema}.{tableName};
            ALTER TABLE {schema}.{tableName} NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE {schema}.{tableName} DISABLE ROW LEVEL SECURITY;
            """;
    }

    /// <summary>
    /// Generates idempotent SQL to create a least-privilege, non-superuser, <c>NOBYPASSRLS</c> runtime role if it does not exist.
    /// </summary>
    public static string GenerateEnsureLeastPrivilegeRoleSql(string tenantRoleName, string password)
    {
        ValidateIdentifier(tenantRoleName, nameof(tenantRoleName));
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (string.Equals(tenantRoleName, "public", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Role name cannot be PUBLIC.", nameof(tenantRoleName));
        }

        var escapedPassword = password.Replace("'", "''", StringComparison.Ordinal);
        return $"""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{tenantRoleName}') THEN
                    CREATE ROLE {tenantRoleName} WITH LOGIN PASSWORD '{escapedPassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
                END IF;
            END
            $$;
            """;
    }

    /// <summary>
    /// Generates least-privilege DML-only grants for the tenant runtime role on a schema.
    /// Explicitly revokes DDL (<c>CREATE</c>) and privileged table actions (<c>TRUNCATE</c>, <c>TRIGGER</c>, <c>REFERENCES</c>).
    /// </summary>
    public static string GenerateLeastPrivilegeTenantRoleGrantsSql(string schema, string tenantRoleName)
    {
        ValidateIdentifier(schema, nameof(schema));
        ValidateIdentifier(tenantRoleName, nameof(tenantRoleName));

        if (string.Equals(tenantRoleName, "public", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Grants must target an explicit runtime role, never PUBLIC.", nameof(tenantRoleName));
        }

        return $"""
            REVOKE ALL ON SCHEMA {schema} FROM {tenantRoleName};
            REVOKE ALL ON ALL TABLES IN SCHEMA {schema} FROM {tenantRoleName};
            REVOKE ALL ON ALL SEQUENCES IN SCHEMA {schema} FROM {tenantRoleName};
            GRANT USAGE ON SCHEMA {schema} TO {tenantRoleName};
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {schema} TO {tenantRoleName};
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {schema} TO {tenantRoleName};
            ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} REVOKE ALL ON TABLES FROM {tenantRoleName};
            ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {tenantRoleName};
            ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} REVOKE ALL ON SEQUENCES FROM {tenantRoleName};
            ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} GRANT USAGE, SELECT ON SEQUENCES TO {tenantRoleName};
            """;
    }

    /// <summary>
    /// Executes RLS commands for specified tables directly on the database.
    /// </summary>
    public static async Task ApplyRlsPoliciesAsync(DbContext context, IEnumerable<(string Schema, string Table)> tables, CancellationToken cancellationToken = default)
    {
        foreach (var (schema, table) in tables)
        {
            var sql = GenerateEnableRlsSql(schema, table);
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private static void ValidateIdentifier(string identifier, string paramName)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !SafePostgresIdentifierRegex().IsMatch(identifier))
        {
            throw new ArgumentException(
                $"Invalid PostgreSQL identifier '{identifier}'. Must match ^[a-z_][a-z0-9_]{{0,62}}$.",
                paramName);
        }
    }
}
