namespace ZynLang.AST.Expressions;

public class InfixExpressionNode(ExpressionNode leftNode, string op, ExpressionNode rightNode) : ExpressionNode
{
    public ExpressionNode LeftNode { get; set; } = leftNode;
    public string Operator { get; set; } = op;
    public ExpressionNode RightNode { get; set; } = rightNode;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "LeftNode", LeftNode.Json() },
            { "Operator", Operator },
            { "RightNode", RightNode.Json() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.InfixExpression;
    }
}
