namespace ZynLang.AST.Expressions;

public class PrefixExpressionNode(string op, ExpressionNode rightNode) : ExpressionNode
{
    public string Operator { get; set; } = op;
    public ExpressionNode RightNode { get; set; } = rightNode;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Operator", Operator },
            { "RightNode", RightNode.Json() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.PrefixExpression;
    }
}
