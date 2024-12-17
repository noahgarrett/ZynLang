using Newtonsoft.Json;

namespace ZynLang.AST.Literals;

public class IdentifierLiteralNode(string value) : ExpressionNode
{
    public string Value { get; set; } = value;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Value", Value },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.IdentifierLiteral;
    }
}
