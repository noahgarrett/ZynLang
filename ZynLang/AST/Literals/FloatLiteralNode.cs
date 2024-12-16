namespace ZynLang.AST.Literals;

public class FloatLiteralNode(float value) : ExpressionNode
{
    public float Value { get; set; } = value;

    public override string Json()
    {
        throw new NotImplementedException();
    }

    public override NodeType Type()
    {
        return NodeType.FloatLiteral;
    }
}
