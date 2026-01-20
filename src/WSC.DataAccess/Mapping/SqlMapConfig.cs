using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace WSC.DataAccess.Mapping;

/// <summary>
/// Configuration for SQL mapping (IBatis-style)
/// </summary>
public class SqlMapConfig
{
    private readonly Dictionary<string, SqlStatement> _statements = new();
    private readonly ILogger<SqlMapConfig> _logger;

    /// <summary>
    /// Creates a new SqlMapConfig instance
    /// </summary>
    public SqlMapConfig(ILogger<SqlMapConfig> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads SQL map from XML file
    /// </summary>
    public void LoadFromXml(string xmlFilePath)
    {
        _logger.LogInformation("Loading SQL map file: {XmlFilePath}", xmlFilePath);

        if (!File.Exists(xmlFilePath))
        {
            _logger.LogError("SQL map file not found: {XmlFilePath}", xmlFilePath);
            throw new FileNotFoundException($"SQL map file not found: {xmlFilePath}");
        }

        try
        {
            var doc = XDocument.Load(xmlFilePath);
            var root = doc.Root;

            if (root == null)
            {
                _logger.LogWarning("SQL map file has no root element: {XmlFilePath}", xmlFilePath);
                return;
            }

            int totalStatements = 0;

            // Parse select statements
            var selectCount = 0;
            foreach (var element in root.Elements("select"))
            {
                var statement = ParseStatement(element, SqlStatementType.Select);
                _statements[statement.Id] = statement;
                selectCount++;
                totalStatements++;
                _logger.LogDebug("Loaded SELECT statement: {StatementId}", statement.Id);
            }

            // Parse insert statements
            var insertCount = 0;
            foreach (var element in root.Elements("insert"))
            {
                var statement = ParseStatement(element, SqlStatementType.Insert);
                _statements[statement.Id] = statement;
                insertCount++;
                totalStatements++;
                _logger.LogDebug("Loaded INSERT statement: {StatementId}", statement.Id);
            }

            // Parse update statements
            var updateCount = 0;
            foreach (var element in root.Elements("update"))
            {
                var statement = ParseStatement(element, SqlStatementType.Update);
                _statements[statement.Id] = statement;
                updateCount++;
                totalStatements++;
                _logger.LogDebug("Loaded UPDATE statement: {StatementId}", statement.Id);
            }

            // Parse delete statements
            var deleteCount = 0;
            foreach (var element in root.Elements("delete"))
            {
                var statement = ParseStatement(element, SqlStatementType.Delete);
                _statements[statement.Id] = statement;
                deleteCount++;
                totalStatements++;
                _logger.LogDebug("Loaded DELETE statement: {StatementId}", statement.Id);
            }

            // Parse procedure statements
            var procedureCount = 0;
            foreach (var element in root.Elements("procedure"))
            {
                var statement = ParseStatement(element, SqlStatementType.Procedure);
                _statements[statement.Id] = statement;
                procedureCount++;
                totalStatements++;
                _logger.LogDebug("Loaded PROCEDURE statement: {StatementId}", statement.Id);
            }

            _logger.LogInformation(
                "Successfully loaded SQL map file: {XmlFilePath}. Total statements: {TotalStatements} (SELECT: {SelectCount}, INSERT: {InsertCount}, UPDATE: {UpdateCount}, DELETE: {DeleteCount}, PROCEDURE: {ProcedureCount})",
                xmlFilePath, totalStatements, selectCount, insertCount, updateCount, deleteCount, procedureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading SQL map file: {XmlFilePath}", xmlFilePath);
            throw;
        }
    }

    /// <summary>
    /// Registers a SQL statement programmatically
    /// </summary>
    public void RegisterStatement(SqlStatement statement)
    {
        if (string.IsNullOrWhiteSpace(statement.Id))
        {
            _logger.LogError("Attempted to register statement with null or empty ID");
            throw new ArgumentException("Statement ID cannot be null or empty");
        }

        var isOverwrite = _statements.ContainsKey(statement.Id);
        _statements[statement.Id] = statement;

        if (isOverwrite)
        {
            _logger.LogWarning("Overwriting existing statement: {StatementId}, Type: {StatementType}",
                statement.Id, statement.StatementType);
        }
        else
        {
            _logger.LogDebug("Registered new statement: {StatementId}, Type: {StatementType}",
                statement.Id, statement.StatementType);
        }
    }

    /// <summary>
    /// Gets a statement by ID
    /// </summary>
    public SqlStatement? GetStatement(string id)
    {
        var found = _statements.TryGetValue(id, out var statement);

        if (!found)
        {
            _logger.LogWarning("Statement not found: {StatementId}", id);
        }
        else
        {
            _logger.LogDebug("Retrieved statement: {StatementId}", id);
        }

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
