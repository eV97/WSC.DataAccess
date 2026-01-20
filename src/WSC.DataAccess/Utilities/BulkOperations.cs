using Dapper;
using WSC.DataAccess.Core;
using System.Data;

namespace WSC.DataAccess.Utilities;

/// <summary>
/// Helper class for bulk database operations
/// </summary>
public static class BulkOperations
{
    /// <summary>
    /// Inserts multiple records in a single transaction
    /// </summary>
    public static async Task<int> BulkInsertAsync<T>(
        IDbSessionFactory sessionFactory,
        string tableName,
        IEnumerable<T> entities,
        Func<T, string, object> parameterMapper) where T : class
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return 0;

        using var session = sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            int totalInserted = 0;

            foreach (var entity in entitiesList)
            {
                var parameters = parameterMapper(entity, tableName);
                var result = await session.Connection.ExecuteAsync(
                    BuildInsertSql(tableName, parameters),
                    parameters,
                    session.Transaction);

                totalInserted += result;
            }

            session.Commit();
            return totalInserted;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Updates multiple records in a single transaction
    /// </summary>
    public static async Task<int> BulkUpdateAsync<T>(
        IDbSessionFactory sessionFactory,
        string tableName,
        string keyColumn,
        IEnumerable<T> entities,
        Func<T, object> parameterMapper) where T : class
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return 0;

        using var session = sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            int totalUpdated = 0;

            foreach (var entity in entitiesList)
            {
                var parameters = parameterMapper(entity);
                var result = await session.Connection.ExecuteAsync(
                    BuildUpdateSql(tableName, keyColumn, parameters),
                    parameters,
                    session.Transaction);

                totalUpdated += result;
            }

            session.Commit();
            return totalUpdated;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Deletes multiple records by IDs in a single transaction
    /// </summary>
    public static async Task<int> BulkDeleteAsync(
        IDbSessionFactory sessionFactory,
        string tableName,
        string keyColumn,
        IEnumerable<object> ids)
    {
        var idsList = ids.ToList();
        if (!idsList.Any())
            return 0;

        using var session = sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            var sql = $"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids";
            var result = await session.Connection.ExecuteAsync(
                sql,
                new { Ids = idsList },
                session.Transaction);

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
    /// Batch inserts with configurable batch size
    /// </summary>
    public static async Task<int> BatchInsertAsync<T>(
        IDbSessionFactory sessionFactory,
        string insertSql,
        IEnumerable<T> entities,
        int batchSize = 1000) where T : class
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return 0;

        int totalInserted = 0;
        var batches = entitiesList.Chunk(batchSize);

        foreach (var batch in batches)
        {
            using var session = sessionFactory.OpenSession();
            session.BeginTransaction();

            try
            {
                var result = await session.Connection.ExecuteAsync(
                    insertSql,
                    batch,
                    session.Transaction);

                session.Commit();
                totalInserted += result;
            }
            catch
            {
                session.Rollback();
                throw;
            }
        }

        return totalInserted;
    }

    /// <summary>
    /// Upsert operation (Insert or Update)
    /// </summary>
    public static async Task<int> UpsertAsync(
        IDbSessionFactory sessionFactory,
        string mergeSql,
        object parameters)
    {
        using var session = sessionFactory.OpenSession();
        session.BeginTransaction();

        try
        {
            var result = await session.Connection.ExecuteAsync(
                mergeSql,
                parameters,
                session.Transaction);

            session.Commit();
            return result;
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }

    #region Private Helper Methods

    private static string BuildInsertSql(string tableName, object parameters)
    {
        var properties = parameters.GetType().GetProperties();
        var columns = string.Join(", ", properties.Select(p => p.Name));
        var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        return $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
    }

    private static string BuildUpdateSql(string tableName, string keyColumn, object parameters)
    {
        var properties = parameters.GetType().GetProperties()
            .Where(p => p.Name != keyColumn);

        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

        return $"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @{keyColumn}";
    }

    #endregion
}

/// <summary>
/// Extension methods for chunking collections
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Splits a collection into chunks of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be greater than 0", nameof(size));

        var list = source.ToList();
        for (int i = 0; i < list.Count; i += size)
        {
            yield return list.Skip(i).Take(size);
        }
    }
}
