namespace ZynLang.AST.Statements;

/// <summary>
/// Ex. `import "test.lime";`
/// Imports all functions contained in a specified file location
/// </summary>
/// <param name="filePath"></param>
public class ImportStatementNode(string filePath) : StatementNode
{
    public string FilePath { get; set; } = filePath;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "FilePath", FilePath }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.ImportStatement;
    }
}
