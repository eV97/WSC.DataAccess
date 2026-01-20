using System.Xml.Linq;

namespace WSC.DataAccess.Mapping;

/// <summary>
/// Configuration for SQL mapping (IBatis-style)
/// </summary>
public class SqlMapConfig
{
    private readonly Dictionary<string, SqlStatement> _statements = new();

    /// <summary>
    /// Loads SQL map from XML file
    /// </summary>
    public void LoadFromXml(string xmlFilePath)
    {
        if (!File.Exists(xmlFilePath))
            throw new FileNotFoundException($"SQL map file not found: {xmlFilePath}");

        var doc = XDocument.Load(xmlFilePath);
        var root = doc.Root;

        if (root == null)
            return;

        // Parse select statements
        foreach (var element in root.Elements("select"))
        {
            var statement = ParseStatement(element, SqlStatementType.Select);
            _statements[statement.Id] = statement;
        }

        // Parse insert statements
        foreach (var element in root.Elements("insert"))
        {
            var statement = ParseStatement(element, SqlStatementType.Insert);
            _statements[statement.Id] = statement;
        }

        // Parse update statements
        foreach (var element in root.Elements("update"))
        {
            var statement = ParseStatement(element, SqlStatementType.Update);
            _statements[statement.Id] = statement;
        }

        // Parse delete statements
        foreach (var element in root.Elements("delete"))
        {
            var statement = ParseStatement(element, SqlStatementType.Delete);
            _statements[statement.Id] = statement;
        }

        // Parse procedure statements
        foreach (var element in root.Elements("procedure"))
        {
            var statement = ParseStatement(element, SqlStatementType.Procedure);
            _statements[statement.Id] = statement;
        }
    }

    /// <summary>
    /// Registers a SQL statement programmatically
    /// </summary>
    public void RegisterStatement(SqlStatement statement)
    {
        if (string.IsNullOrWhiteSpace(statement.Id))
            throw new ArgumentException("Statement ID cannot be null or empty");

        _statements[statement.Id] = statement;
    }

    /// <summary>
    /// Gets a statement by ID
    /// </summary>
    public SqlStatement? GetStatement(string id)
    {
        _statements.TryGetValue(id, out var statement);
        return statement;
    }

    /// <summary>
    /// Gets all registered statements
    /// </summary>
    public IReadOnlyDictionary<string, SqlStatement> GetAllStatements()
    {
        return _statements;
    }

    private SqlStatement ParseStatement(XElement element, SqlStatementType type)
    {
        var id = element.Attribute("id")?.Value ?? string.Empty;
        var resultType = element.Attribute("resultType")?.Value;
        var parameterType = element.Attribute("parameterType")?.Value;
        var timeout = element.Attribute("timeout")?.Value;

        var statement = new SqlStatement
        {
            Id = id,
            CommandText = element.Value.Trim(),
            StatementType = type,
            ResultType = resultType != null ? Type.GetType(resultType) : null,
            ParameterType = parameterType != null ? Type.GetType(parameterType) : null,
            CommandTimeout = timeout != null ? int.Parse(timeout) : null
        };

        return statement;
    }
}
