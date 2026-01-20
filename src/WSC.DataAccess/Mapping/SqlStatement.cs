namespace WSC.DataAccess.Mapping;

/// <summary>
/// Represents a SQL statement configuration (similar to IBatis statement mapping)
/// </summary>
public class SqlStatement
{
    public string Id { get; set; } = string.Empty;
    public string CommandText { get; set; } = string.Empty;
    public SqlStatementType StatementType { get; set; } = SqlStatementType.Unknown;
    public Type? ResultType { get; set; }
    public Type? ParameterType { get; set; }
    public int? CommandTimeout { get; set; }
}

/// <summary>
/// Types of SQL statements
/// </summary>
public enum SqlStatementType
{
    Unknown = 0,
    Select = 1,
    Insert = 2,
    Update = 3,
    Delete = 4,
    Procedure = 5
}
