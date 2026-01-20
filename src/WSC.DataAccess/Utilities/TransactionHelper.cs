using System.Data;
using WSC.DataAccess.Core;

namespace WSC.DataAccess.Utilities;

/// <summary>
/// Helper class for transaction management
/// </summary>
public static class TransactionHelper
{
    /// <summary>
    /// Executes multiple operations within a single transaction
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="sessionFactory">Session factory</param>
    /// <param name="operation">Operation to execute</param>
    /// <param name="isolationLevel">Transaction isolation level</param>
    /// <returns>Result of the operation</returns>
    public static async Task<T> ExecuteInTransactionAsync<T>(
        IDbSessionFactory sessionFactory,
        Func<DbSession, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        using var session = sessionFactory.OpenSession();
        session.BeginTransaction(isolationLevel);

        try
        {
            var result = await operation(session);
            session.Commit();
            return result;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes multiple operations within a single transaction (void return)
    /// </summary>
    public static async Task ExecuteInTransactionAsync(
        IDbSessionFactory sessionFactory,
        Func<DbSession, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        using var session = sessionFactory.OpenSession();
        session.BeginTransaction(isolationLevel);

        try
        {
            await operation(session);
            session.Commit();
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes a batch of operations in a single transaction
    /// </summary>
    public static async Task ExecuteBatchAsync(
        IDbSessionFactory sessionFactory,
        params Func<DbSession, Task>[] operations)
    {
        using var session = sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            foreach (var operation in operations)
            {
                await operation(session);
            }
            session.Commit();
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Retries a transaction operation on deadlock
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        IDbSessionFactory sessionFactory,
        Func<DbSession, Task<T>> operation,
        int maxRetries = 3,
        int delayMilliseconds = 100)
    {
        int attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                return await ExecuteInTransactionAsync(sessionFactory, operation);
            }
            catch (Exception ex) when (IsDeadlockException(ex) && attempt < maxRetries)
            {
                // Wait before retry with exponential backoff
                await Task.Delay(delayMilliseconds * attempt);
            }
        }
    }

    /// <summary>
    /// Checks if an exception is a deadlock exception
    /// </summary>
    private static bool IsDeadlockException(Exception ex)
    {
        // SQL Server deadlock error number is 1205
        return ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("1205");
    }
}
