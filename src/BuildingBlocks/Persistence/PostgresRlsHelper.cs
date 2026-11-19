using Microsoft.EntityFrameworkCore;

namespace SchoolOS.BuildingBlocks.Persistence;

/// <summary>
/// Utility for generating and applying PostgreSQL Row-Level Security (RLS) policies.
/// </summary>
public static class PostgresRlsHelper
{
    /// <summary>
    /// Generates idempotent SQL commands to enable and enforce tenant isolation via PostgreSQL RLS.
    /// When <paramref name="allowPlatformAdminBypass"/> is true (for Cloud Control Plane commercial tables),
    /// verified platform-admin / lifecycle-job transactions with <c>app.is_platform_admin = 'true'</c> may operate cross-tenant.
    /// </summary>
    public static string GenerateEnableRlsSql(
        string schema,
        string tableName,
        string tenantIdColumn = "tenant_id",
        bool allowPlatformAdminBypass = false)
    {
        var policyName = $"{tableName}_tenant_isolation";
        var predicate = allowPlatformAdminBypass
            ? $"(current_setting('app.is_platform_admin', true) = 'true' OR {tenantIdColumn} = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)"
            : $"({tenantIdColumn} = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)";

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
    /// Generates idempotent SQL commands to drop RLS policies and disable RLS during migration rollback.
    /// </summary>
    public static string GenerateDisableRlsSql(string schema, string tableName)
    {
        var policyName = $"{tableName}_tenant_isolation";
        return $"""
            DROP POLICY IF EXISTS {policyName} ON {schema}.{tableName};
            ALTER TABLE {schema}.{tableName} NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE {schema}.{tableName} DISABLE ROW LEVEL SECURITY;
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
}
