using Microsoft.EntityFrameworkCore;

namespace SchoolOS.BuildingBlocks.Persistence;

/// <summary>
/// Utility for generating and applying PostgreSQL Row-Level Security (RLS) policies.
/// </summary>
public static class PostgresRlsHelper
{
    /// <summary>
    /// Generates idempotent SQL commands to enable and enforce tenant isolation via PostgreSQL RLS.
    /// </summary>
    public static string GenerateEnableRlsSql(string schema, string tableName, string tenantIdColumn = "tenant_id")
    {
        var policyName = $"{tableName}_tenant_isolation";
        return $"""
            ALTER TABLE {schema}.{tableName} ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {schema}.{tableName} FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS {policyName} ON {schema}.{tableName};
            CREATE POLICY {policyName} ON {schema}.{tableName}
                FOR ALL
                USING ({tenantIdColumn} = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK ({tenantIdColumn} = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
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
