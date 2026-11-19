using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolOS.BuildingBlocks.Security;

namespace SchoolOS.BuildingBlocks.Persistence.Interceptors;

/// <summary>
/// EF Core DbTransactionInterceptor that scopes PostgreSQL tenant context transaction-locally
/// using `set_config('app.current_tenant_id', @tenant_id, true)`.
/// Because is_local = true, PostgreSQL automatically drops the configuration parameter
/// whenever the transaction commits, rolls back, or is aborted, guaranteeing zero context leakage
/// across connection pool reuses.
/// </summary>
public class PostgresTenantTransactionInterceptor : DbTransactionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public PostgresTenantTransactionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default)
    {
        await SetTransactionLocalTenantAsync(result, cancellationToken);
        return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
    }

    public override DbTransaction TransactionStarted(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result)
    {
        SetTransactionLocalTenant(result);
        return base.TransactionStarted(connection, eventData, result);
    }

    private async Task SetTransactionLocalTenantAsync(DbTransaction transaction, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId?.ToString() ?? string.Empty;
        if (transaction.Connection is null)
        {
            return;
        }

        await using var cmd = transaction.Connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant_id, true);";
        var param = cmd.CreateParameter();
        param.ParameterName = "@tenant_id";
        param.Value = tenantId;
        cmd.Parameters.Add(param);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private void SetTransactionLocalTenant(DbTransaction transaction)
    {
        var tenantId = _tenantContext.TenantId?.ToString() ?? string.Empty;
        if (transaction.Connection is null)
        {
            return;
        }

        using var cmd = transaction.Connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant_id, true);";
        var param = cmd.CreateParameter();
        param.ParameterName = "@tenant_id";
        param.Value = tenantId;
        cmd.Parameters.Add(param);
        cmd.ExecuteNonQuery();
    }
}
