namespace ZynLang.AST.Literals;

public class StringLiteralNode(string value) : ExpressionNode
{
    public string Value { get; set; } = value;

    public override string Json()
    {
        throw new NotImplementedException();
    }

    public override NodeType Type()
    {
        return NodeType.StringLiteral;
    }
}
