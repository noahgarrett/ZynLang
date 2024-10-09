namespace ZynLang.AST.Literals;

public class IntegerLiteralNode(int value) : ExpressionNode
{
    public int Value { get; set; } = value;

    public override NodeType Type()
    {
        return NodeType.IntegerLiteral;
    }
}
