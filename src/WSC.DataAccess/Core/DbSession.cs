using System.Data;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<DbSession>? _logger;
    private readonly string _sessionId;

    public DbSession(IDbConnection connection, ILogger<DbSession>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger;
        _sessionId = Guid.NewGuid().ToString("N")[..8];

        _logger?.LogDebug("DbSession created - SessionId: {SessionId}, Database: {Database}",
            _sessionId, _connection.Database);

        if (_connection.State != ConnectionState.Open)
        {
            _logger?.LogDebug("Opening connection - SessionId: {SessionId}", _sessionId);
            _connection.Open();
            _logger?.LogInformation("Connection opened - SessionId: {SessionId}, Database: {Database}",
                _sessionId, _connection.Database);
        }
        else
        {
            _logger?.LogDebug("Connection already open - SessionId: {SessionId}", _sessionId);
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
        {
            _logger?.LogError("Attempted to start transaction when one is already active - SessionId: {SessionId}", _sessionId);
            throw new InvalidOperationException("Transaction already started");
        }

        _logger?.LogDebug("Beginning transaction - SessionId: {SessionId}", _sessionId);
        _transaction = _connection.BeginTransaction();
        _logger?.LogInformation("Transaction started - SessionId: {SessionId}, IsolationLevel: {IsolationLevel}",
            _sessionId, _transaction.IsolationLevel);
    }

    /// <summary>
    /// Begins a new database transaction with specified isolation level
    /// </summary>
    public void BeginTransaction(IsolationLevel isolationLevel)
    {
        if (_transaction != null)
        {
            _logger?.LogError("Attempted to start transaction when one is already active - SessionId: {SessionId}", _sessionId);
            throw new InvalidOperationException("Transaction already started");
        }

        _logger?.LogDebug("Beginning transaction with isolation level {IsolationLevel} - SessionId: {SessionId}",
            isolationLevel, _sessionId);
        _transaction = _connection.BeginTransaction(isolationLevel);
        _logger?.LogInformation("Transaction started - SessionId: {SessionId}, IsolationLevel: {IsolationLevel}",
            _sessionId, isolationLevel);
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    public void Commit()
    {
        if (_transaction == null)
        {
            _logger?.LogError("Attempted to commit when no transaction is active - SessionId: {SessionId}", _sessionId);
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            _logger?.LogDebug("Committing transaction - SessionId: {SessionId}", _sessionId);
            _transaction.Commit();
            _logger?.LogInformation("Transaction committed successfully - SessionId: {SessionId}", _sessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Transaction commit failed - SessionId: {SessionId}", _sessionId);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    public void Rollback()
    {
        if (_transaction == null)
        {
            _logger?.LogError("Attempted to rollback when no transaction is active - SessionId: {SessionId}", _sessionId);
            throw new InvalidOperationException("No transaction to rollback");
        }

        try
        {
            _logger?.LogDebug("Rolling back transaction - SessionId: {SessionId}", _sessionId);
            _transaction.Rollback();
            _logger?.LogWarning("Transaction rolled back - SessionId: {SessionId}", _sessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Transaction rollback failed - SessionId: {SessionId}", _sessionId);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
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

        _logger?.LogDebug("Disposing DbSession - SessionId: {SessionId}", _sessionId);

        if (_transaction != null)
        {
            _logger?.LogWarning("Transaction not committed or rolled back before dispose - SessionId: {SessionId}. Rolling back automatically.", _sessionId);
            try
            {
                _transaction.Rollback();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rolling back transaction during dispose - SessionId: {SessionId}", _sessionId);
            }
            _transaction?.Dispose();
        }

        if (_connection.State == ConnectionState.Open)
        {
            _logger?.LogDebug("Closing connection - SessionId: {SessionId}", _sessionId);
            _connection.Close();
            _logger?.LogInformation("Connection closed - SessionId: {SessionId}", _sessionId);
        }

        _connection.Dispose();
        _disposed = true;
        _logger?.LogDebug("DbSession disposed - SessionId: {SessionId}", _sessionId);
    }
}
