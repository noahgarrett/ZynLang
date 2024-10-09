namespace ZynLang.AST.Expressions;

public class PrefixExpressionNode(string op, ExpressionNode rightNode) : ExpressionNode
{
    public string Operator { get; set; } = op;
    public ExpressionNode RightNode { get; set; } = rightNode;

    public override NodeType Type()
    {
        return NodeType.PrefixExpression;
    }
}
