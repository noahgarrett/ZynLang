namespace ZynLang.AST.Expressions;

public class IndexExpressionNode(ExpressionNode leftNode, ExpressionNode indexNode) : ExpressionNode
{
    public ExpressionNode LeftNode { get; set; } = leftNode;
    public ExpressionNode IndexNode { get; set; } = indexNode;

    public override NodeType Type()
    {
        return NodeType.IndexExpression;
    }
}
