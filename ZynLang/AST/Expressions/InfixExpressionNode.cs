namespace ZynLang.AST.Expressions;

public class InfixExpressionNode(ExpressionNode leftNode, string op, ExpressionNode rightNode) : ExpressionNode
{
    public ExpressionNode LeftNode { get; set; } = leftNode;
    public string Operator { get; set; } = op;
    public ExpressionNode RightNode { get; set; } = rightNode;

    public override string Json()
    {
        throw new NotImplementedException();
    }

    public override NodeType Type()
    {
        return NodeType.InfixExpression;
    }
}
