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

    public override string Json()
    {
        dynamic obj = new ExpandoObject();
        obj.Type = Type().ToString();
        obj.Value = Value;

        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}
