namespace ZynLang.AST.Statements;

public class WhileStatementNode(ExpressionNode condition, BlockStatementNode body) : StatementNode
{
    public ExpressionNode Condition { get; set; } = condition;
    public BlockStatementNode Body { get; set; } = body;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Condition", Condition.Json() },
            { "Body", Body.Json() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.WhileStatement;
    }
}
