namespace ZynLang.AST.Statements;

public class WhileStatementNode(ExpressionNode condition, BlockStatementNode body) : StatementNode
{
    public ExpressionNode Condition { get; set; } = condition;
    public BlockStatementNode Body { get; set; } = body;

    public override NodeType Type()
    {
        return NodeType.WhileStatement;
    }
}
