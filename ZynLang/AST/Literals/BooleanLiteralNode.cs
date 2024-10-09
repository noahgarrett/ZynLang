namespace ZynLang.AST.Literals;

public class BooleanLiteralNode(bool value) : ExpressionNode
{
    public bool Value { get; set; } = value;

    public override NodeType Type()
    {
        return NodeType.BooleanLiteral;
    }
}
