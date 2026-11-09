using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// Interceptor that establishes the active tenant context directly in PostgreSQL
/// using `set_config('app.current_tenant_id', ...)`.
/// This powers PostgreSQL Row-Level Security (RLS) policies and ensures pooled
/// database connections fail closed if tenant context is missing.
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
}
