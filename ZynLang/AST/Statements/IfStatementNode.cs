namespace ZynLang.AST.Statements;

public class IfStatementNode(ExpressionNode condition, BlockStatementNode consequence, BlockStatementNode? alternative) : StatementNode
{
    public ExpressionNode Condition { get; set; } = condition;
    public BlockStatementNode Consequence { get; set; } = consequence;
    public BlockStatementNode? Alternative { get; set; } = alternative;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Condition", Condition.Json() },
            { "Consequence", Consequence.Json() },
            { "Alternative", Alternative == null ? null : Alternative.Json() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.IfStatement;
    }
}
