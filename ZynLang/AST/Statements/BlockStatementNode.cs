namespace ZynLang.AST.Statements;

public class BlockStatementNode(List<StatementNode> statements) : Node
{
    public List<StatementNode> Statements { get; set; } = statements;

    public override NodeType Type()
    {
        return NodeType.BlockStatement;
    }
}
