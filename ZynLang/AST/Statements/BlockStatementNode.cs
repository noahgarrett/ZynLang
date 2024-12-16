namespace ZynLang.AST.Statements;

public class BlockStatementNode(List<StatementNode> statements) : StatementNode
{
    public List<StatementNode> Statements { get; set; } = statements;

    public override string Json()
    {
        throw new NotImplementedException();
    }

    public override NodeType Type()
    {
        return NodeType.BlockStatement;
    }
}
