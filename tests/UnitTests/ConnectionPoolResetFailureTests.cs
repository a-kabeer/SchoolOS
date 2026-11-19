using System.Data;
using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SchoolOS.BuildingBlocks.Persistence.Interceptors;
using SchoolOS.BuildingBlocks.Security;
using Xunit;

namespace SchoolOS.UnitTests;

public class ConnectionPoolResetFailureTests
{
    private class FaultyDbConnection : DbConnection
    {
        private ConnectionState _state;
        public int CloseCallCount { get; private set; }

        public FaultyDbConnection(ConnectionState state = ConnectionState.Open)
        {
            _state = state;
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = "Host=localhost;Database=test";
        public override string Database => "test";
        public override string DataSource => "localhost";
        public override string ServerVersion => "17.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }
        public override void Open() => _state = ConnectionState.Open;

        public override void Close()
        {
            CloseCallCount++;
            _state = ConnectionState.Closed;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();

        protected override DbCommand CreateDbCommand()
        {
            return new FaultyDbCommand(this);
        }
    }

    private class FaultyDbCommand : DbCommand
    {
        private readonly DbConnection _connection;

        public FaultyDbCommand(DbConnection connection)
        {
            _connection = connection;
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get => _connection; set { } }
        protected override DbParameterCollection DbParameterCollection => new FaultyParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new Npgsql.PostgresException("Simulated connection timeout during reset", "57P01", "operator intervention", "fatal");
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => throw new Npgsql.PostgresException("Simulated connection timeout during reset", "57P01", "operator intervention", "fatal");
        public override object? ExecuteScalar() => throw new NotImplementedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FaultyParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
    }

    private class FaultyParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private class FaultyParameterCollection : DbParameterCollection
    {
        private readonly List<object?> _list = new();
        public override int Count => _list.Count;
        public override object SyncRoot => this;
        public override int Add(object? value) { _list.Add(value); return _list.Count - 1; }
        public override void AddRange(Array values) { foreach (var v in values) _list.Add(v); }
        public override void Clear() => _list.Clear();
        public override bool Contains(object? value) => _list.Contains(value);
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) => _list.ToArray().CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
        protected override DbParameter GetParameter(int index) => (DbParameter)_list[index]!;
        protected override DbParameter GetParameter(string parameterName) => (DbParameter)_list[0]!;
        public override int IndexOf(object? value) => _list.IndexOf(value);
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object? value) => _list.Insert(index, value);
        public override void Remove(object? value) => _list.Remove(value);
        public override void RemoveAt(int index) => _list.RemoveAt(index);
        public override void RemoveAt(string parameterName) { }
        protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }

    [Fact]
    public async Task ResetTenantSessionVariableAsync_Throws_And_Preserves_Original_Exception_When_Reset_Fails()
    {
        var tenantId = Guid.NewGuid();
        var context = new TenantContext(tenantId, null, "user-test");
        var interceptor = new PostgresTenantSessionInterceptor(context);

        var faultyConn = new FaultyDbConnection(ConnectionState.Open);
        var eventData = new ConnectionEventData(
            null!,
            (_, _) => string.Empty,
            faultyConn,
            null,
            Guid.NewGuid(),
            false,
            DateTimeOffset.UtcNow);

        // Act: Attempt to intercept connection closing on a connection where set_config throws
        var act = async () => await interceptor.ConnectionClosingAsync(faultyConn, eventData, default);

        // Assert:
        // 1. Must NOT swallow exception
        // 2. Must throw InvalidOperationException
        // 3. Must preserve the original inner exception
        // 4. Must NOT call connection.Close() recursively inside the interceptor callback
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*CRITICAL: Failed to reset PostgreSQL tenant session variable 'app.current_tenant_id'*");
        ex.WithInnerException<Npgsql.PostgresException>();

        faultyConn.CloseCallCount.Should().Be(0, "Close() must not be called recursively from inside ConnectionClosingAsync");
    }

    [Fact]
    public void ResetTenantSessionVariable_Synchronous_Throws_And_Preserves_Original_Exception_When_Reset_Fails()
    {
        var tenantId = Guid.NewGuid();
        var context = new TenantContext(tenantId, null, "user-test");
        var interceptor = new PostgresTenantSessionInterceptor(context);

        var faultyConn = new FaultyDbConnection(ConnectionState.Open);
        var eventData = new ConnectionEventData(
            null!,
            (_, _) => string.Empty,
            faultyConn,
            null,
            Guid.NewGuid(),
            false,
            DateTimeOffset.UtcNow);

        // Act: Synchronous closing interception
        var act = () => interceptor.ConnectionClosing(faultyConn, eventData, default);

        var ex = act.Should().Throw<InvalidOperationException>();
        ex.WithMessage("*CRITICAL: Failed to reset PostgreSQL tenant session variable 'app.current_tenant_id'*");
        ex.WithInnerException<Npgsql.PostgresException>();

        faultyConn.CloseCallCount.Should().Be(0, "Close() must not be called recursively from inside ConnectionClosing");
    }

    [Fact]
    public async Task ResetTenantSessionVariableAsync_Does_Not_Throw_When_Connection_Is_Already_Closed()
    {
        var tenantId = Guid.NewGuid();
        var context = new TenantContext(tenantId, null, "user-test");
        var interceptor = new PostgresTenantSessionInterceptor(context);

        // Connection is already closed / broken
        var closedConn = new FaultyDbConnection(ConnectionState.Closed);
        var eventData = new ConnectionEventData(
            null!,
            (_, _) => string.Empty,
            closedConn,
            null,
            Guid.NewGuid(),
            false,
            DateTimeOffset.UtcNow);

        // Should cleanly return without attempting command execution on a closed connection
        var act = async () => await interceptor.ConnectionClosingAsync(closedConn, eventData, default);
        await act.Should().NotThrowAsync();
    }
}
