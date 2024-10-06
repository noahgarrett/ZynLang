namespace ZynLang.AST.Statements;

public class IfStatementNode(ExpressionNode condition, BlockStatementNode consequence, BlockStatementNode alternative) : Node
{
    public ExpressionNode Condition { get; set; } = condition;
    public BlockStatementNode Consequence { get; set; } = consequence;
    public BlockStatementNode Alternative { get; set; } = alternative;

    public override NodeType Type()
    {
        return NodeType.IfStatement;
    }
}
