using System.Data;

namespace WSC.DataAccess.Core;

/// <summary>
/// Represents a database session with connection and transaction management
/// Similar to IBatis SqlMapSession
/// </summary>
public class DbSession : IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public DbSession(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    /// <summary>
    /// Gets the current database connection
    /// </summary>
    public IDbConnection Connection => _connection;

    /// <summary>
    /// Gets the current transaction
    /// </summary>
    public IDbTransaction? Transaction => _transaction;

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    public void BeginTransaction()
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started");

        _transaction = _connection.BeginTransaction();
    }

    /// <summary>
    /// Begins a new database transaction with specified isolation level
    /// </summary>
    public void BeginTransaction(IsolationLevel isolationLevel)
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started");

        _transaction = _connection.BeginTransaction(isolationLevel);
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    public void Commit()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to commit");

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    public void Rollback()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to rollback");

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }

    /// <summary>
    /// Creates a command for this session
    /// </summary>
    public IDbCommand CreateCommand()
    {
        var command = _connection.CreateCommand();
        if (_transaction != null)
        {
            command.Transaction = _transaction;
        }
        return command;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();

        if (_connection.State == ConnectionState.Open)
        {
            _connection.Close();
        }

        _connection.Dispose();
        _disposed = true;
    }
}
