using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Interceptor that establishes the active tenant context directly in PostgreSQL
/// using `set_config('app.current_tenant_id', ...)`.
/// This powers PostgreSQL Row-Level Security (RLS) policies and ensures pooled
/// database connections fail closed if tenant context is missing.
/// On connection close, it explicitly resets the session variable to prevent any cross-request context leakage.
/// </summary>
public class PostgresTenantSessionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public PostgresTenantSessionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantSessionVariableAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantSessionVariable(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        await ResetTenantSessionVariableAsync(connection);
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }

    public override InterceptionResult ConnectionClosing(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        ResetTenantSessionVariable(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    private async Task SetTenantSessionVariableAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId?.ToString() ?? string.Empty;
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant_id, false);";
        var param = cmd.CreateParameter();
        param.ParameterName = "@tenant_id";
        param.Value = tenantId;
        cmd.Parameters.Add(param);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private void SetTenantSessionVariable(DbConnection connection)
    {
        var tenantId = _tenantContext.TenantId?.ToString() ?? string.Empty;
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant_id, false);";
        var param = cmd.CreateParameter();
        param.ParameterName = "@tenant_id";
        param.Value = tenantId;
        cmd.Parameters.Add(param);
        cmd.ExecuteNonQuery();
    }

    private async Task ResetTenantSessionVariableAsync(DbConnection connection)
    {
        if (connection.State == ConnectionState.Open)
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT set_config('app.current_tenant_id', '', false);";
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Never swallow reset failures: fail closed and invalidate physical connection
                try
                {
                    connection.Close();
                }
                catch
                {
                    // Ignore secondary exception during close attempt
                }

                throw new InvalidOperationException(
                    "CRITICAL: Failed to reset PostgreSQL tenant session variable 'app.current_tenant_id' before returning connection to pool. " +
                    "Connection was closed to prevent cross-tenant context leakage.", ex);
            }
        }
    }

    private void ResetTenantSessionVariable(DbConnection connection)
    {
        if (connection.State == ConnectionState.Open)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT set_config('app.current_tenant_id', '', false);";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Never swallow reset failures: fail closed and invalidate physical connection
                try
                {
                    connection.Close();
                }
                catch
                {
                    // Ignore secondary exception during close attempt
                }

                throw new InvalidOperationException(
                    "CRITICAL: Failed to reset PostgreSQL tenant session variable 'app.current_tenant_id' before returning connection to pool. " +
                    "Connection was closed to prevent cross-tenant context leakage.", ex);
            }
        }
    }
}
