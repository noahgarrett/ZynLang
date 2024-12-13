namespace ZynLang.AST.Statements;

/// <summary>
/// Ex. `import "test.lime";`
/// Imports all functions contained in a specified file location
/// </summary>
/// <param name="filePath"></param>
public class ImportStatementNode(string filePath) : StatementNode
{
    public string FilePath { get; set; } = filePath;

    public override NodeType Type()
    {
        return NodeType.ImportStatement;
    }
}
