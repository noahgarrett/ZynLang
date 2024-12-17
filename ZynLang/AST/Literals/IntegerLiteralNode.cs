using Newtonsoft.Json;
using System.Dynamic;

namespace ZynLang.AST.Literals;

public class IntegerLiteralNode(int value) : ExpressionNode
{
    public int Value { get; set; } = value;

    public override NodeType Type()
    {
        return NodeType.IntegerLiteral;
    }

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Value", Value }
        };

        return obj;
    }
}
