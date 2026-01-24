namespace WSC.DataAccess.Attributes;

/// <summary>
/// Attribute để chỉ định SQL map file cho repository
/// Sử dụng: [SqlMapFile(SqlMapFiles.DAO005)]
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SqlMapFileAttribute : Attribute
{
    /// <summary>
    /// SQL map file path
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filePath">SQL map file path (ví dụ: SqlMapFiles.DAO005)</param>
    public SqlMapFileAttribute(string filePath)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }
}

/// <summary>
/// Attribute để chỉ định nhiều SQL map files cho repository
/// Sử dụng: [SqlMapFiles(SqlMapFiles.DAO005, SqlMapFiles.DAO006)]
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SqlMapFilesAttribute : Attribute
{
    /// <summary>
    /// SQL map file paths
    /// </summary>
    public string[] FilePaths { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filePaths">SQL map file paths</param>
    public SqlMapFilesAttribute(params string[] filePaths)
    {
        FilePaths = filePaths ?? throw new ArgumentNullException(nameof(filePaths));
    }
}
