namespace ZynLang.AST.Literals;

public class ArrayLiteralNode(List<ExpressionNode> elements) : ExpressionNode
{
    public List<ExpressionNode> Elements { get; set; } = elements;

    public override NodeType Type()
    {
        return NodeType.ArrayLiteral;
    }
}
