namespace ZynLang.AST.Literals;

public class IdentifierLiteralNode(string value) : Node
{
    public string Value { get; set; } = value;

    public override NodeType Type()
    {
        return NodeType.IdentifierLiteral;
    }
}
