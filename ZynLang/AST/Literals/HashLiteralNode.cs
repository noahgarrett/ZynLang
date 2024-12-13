namespace ZynLang.AST.Literals;

public class HashLiteralNode(Dictionary<ExpressionNode, ExpressionNode> pairs) : ExpressionNode
{
    public Dictionary<ExpressionNode, ExpressionNode> Pairs { get; set; } = pairs;

    public override NodeType Type()
    {
        return NodeType.HashLiteral;
    }
}
