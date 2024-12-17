namespace ZynLang.AST.Statements;

public class ExpressionStatementNode(ExpressionNode expr) : StatementNode
{
    public ExpressionNode Expression { get; set; } = expr;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Expression", Expression.Json() }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.ExpressionStatement;
    }
}
