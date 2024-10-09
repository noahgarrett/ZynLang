namespace ZynLang.AST.Statements;

public class ImportStatementNode(string filePath) : StatementNode
{
    public string FilePath { get; set; } = filePath;

    public override NodeType Type()
    {
        return NodeType.ImportStatement;
    }
}
