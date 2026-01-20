namespace WSC.DataAccess.Core;

/// <summary>
/// Factory for creating database sessions
/// Similar to IBatis SqlMapper
/// </summary>
public interface IDbSessionFactory
{
    /// <summary>
    /// Opens a new database session
    /// </summary>
    DbSession OpenSession();

    /// <summary>
    /// Opens a new database session with specific connection string name
    /// </summary>
    DbSession OpenSession(string connectionName);
}
